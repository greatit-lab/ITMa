// Services\FileWatcherManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ITM_Agent.Services
{
    /// <summary>
    /// FileSystemWatcher를 사용하여 지정된 타겟 폴더(TargetFolders)를 감시하고,
    /// 발생하는 파일 변경 이벤트에 따라 정규표현식(Regex) 패턴 매칭 후 파일을 지정된 폴더로 복사하는 로직을 담당합니다.
    /// </summary>
    public class FileWatcherManager
    {
        
        private SettingsManager settingsManager;
        private LogManager logManager;
        private bool isDebugMode;  // readonly 제거
        private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private readonly Dictionary<string, DateTime> lastModifiedFiles = new Dictionary<string, DateTime>(); // 수정 시간 추적
        private readonly HashSet<string> recentlyCreatedFiles = new HashSet<string>(); // 최근 생성된 파일 추적
        private readonly HashSet<string> deletedFiles = new HashSet<string>(); // 삭제된 파일 추적
        private readonly Dictionary<string, DateTime> fileProcessTracker = new Dictionary<string, DateTime>(); // 파일 처리 추적
        private readonly TimeSpan duplicateEventThreshold = TimeSpan.FromSeconds(5); // 중복 이벤트 방지 시간
        
        private bool isRunning = false;
        
        // Debug Mode 상태 속성
        public bool IsDebugMode { get; set; } = false;
    
    public FileWatcherManager(SettingsManager settingsManager, LogManager logManager, bool isDebugMode)
    {
       this.settingsManager = settingsManager;
       this.logManager = logManager;
       this.isDebugMode = isDebugMode;  // 이제 정상 할당 가능
    }
    
    public void UpdateDebugMode(bool isDebug)
        {
            this.isDebugMode = isDebug; // 디버그 모드 상태 업데이트
        }

        public void InitializeWatchers()
        {
            StopWatchers(); // 기존 Watcher 중지
            var targetFolders = settingsManager.GetFoldersFromSection("[TargetFolders]");
            if (targetFolders.Count == 0)
            {
                logManager.LogEvent("[FileWatcherManager] No target folders configured for monitoring.");
                return;
            }

            foreach (var folder in targetFolders)
            {
                if (!Directory.Exists(folder))
                {
                    logManager.LogEvent($"[FileWatcherManager] Folder does not exist: {folder}", true);
                    continue;
                }

                var watcher = new FileSystemWatcher
                {
                    Path = folder,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
                };

                watcher.Created += OnFileChanged;
                watcher.Changed += OnFileChanged;
                watcher.Deleted += OnFileChanged;

                watchers.Add(watcher);

                if (isDebugMode)
                {
                    logManager.LogDebug($"[FileWatcherManager] Initialized watcher for folder: {folder}");
                }
            }

            logManager.LogEvent($"[FileWatcherManager] {watchers.Count} watcher(s) initialized.");
        }

        public void StartWatching()
        {
           if (isRunning)
           {
               logManager.LogEvent("[FileWatcherManager] File monitoring is already running.");
               return;
           }
        
           InitializeWatchers(); // 새로 초기화
        
           foreach (var watcher in watchers)
           {
               watcher.EnableRaisingEvents = true; // 이벤트 활성화
           }
        
           isRunning = true; // 상태 업데이트
           logManager.LogEvent("[FileWatcherManager] File monitoring started.");
           if (settingsManager.IsDebugMode)
           {
               logManager.LogDebug(
                   $"[FileWatcherManager] Monitoring {watchers.Count} folder(s): " +
                   $"{string.Join(", ", watchers.Select(w => w.Path))}"
               );
           }
        }

        public void StopWatchers()
        {
            foreach (var w in watchers)
            {
                w.EnableRaisingEvents = false;
                w.Created -= OnFileChanged;
                w.Changed -= OnFileChanged;
                w.Deleted -= OnFileChanged;
                w.Dispose();
            }
            watchers.Clear(); // 리스트 비우기
            isRunning = false; // 상태 업데이트
            logManager.LogEvent("[FileWatcherManager] File monitoring stopped.");
        }

        private async Task<bool> WaitForFileReadyAsync(string filePath, int maxRetries, int delayMilliseconds)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (IsFileReady(filePath))
                {
                    logManager.LogEvent($"[FileWatcherManager] File is ready: {filePath}");
                    return true;
                }

                logManager.LogEvent($"[FileWatcherManager] Retrying... File not ready: {filePath}, Attempt: {attempt + 1}");
                await Task.Delay(delayMilliseconds);
            }

            logManager.LogEvent($"[FileWatcherManager] File not ready after {maxRetries} attempts: {filePath}");
            return false;
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!isRunning)
            {
                if (isDebugMode)
                    logManager.LogDebug($"[FileWatcherManager] File event ignored (not running): {e.FullPath}");
                return;
            }

            // 중복 이벤트 방지
            if (IsDuplicateEvent(e.FullPath))
            {
                if (isDebugMode)
                    logManager.LogDebug($"[FileWatcherManager] Duplicate event ignored: {e.ChangeType} - {e.FullPath}");
                return;
            }

            try
            {
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    if (File.Exists(e.FullPath))
                    {
                        await Task.Run(() => ProcessFile(e.FullPath));
                    }
                }
                else if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    logManager.LogEvent($"[FileWatcherManager] File Deleted: {e.FullPath}");
                }
            }
            catch (Exception ex)
            {
                logManager.LogEvent($"[FileWatcherManager] Error processing file: {e.FullPath}. Exception: {ex.Message}");
                if (isDebugMode)
                {
                    logManager.LogDebug($"[FileWatcherManager] Exception details: {ex.Message}");
                }
            }
        }

        private async Task<string> ProcessFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            var regexList = settingsManager.GetRegexList();
        
            foreach (var kvp in regexList)
            {
                if (Regex.IsMatch(fileName, kvp.Key))
                {
                    string destinationFolder = kvp.Value;
                    string destinationFile = Path.Combine(destinationFolder, fileName);
        
                    try
                    {
                        Directory.CreateDirectory(destinationFolder);
        
                        if (!await WaitForFileReady(filePath))
                        {
                            logManager.LogEvent($"[FileWatcherManager] File skipped (not ready): {fileName}");
                            return null;
                        }
        
                        File.Copy(filePath, destinationFile, true);
        
                        // ▼▼▼ 원본 로그 (전체 경로 출력) ▼▼▼
                        // logManager.LogEvent($"[FileWatcherManager] File Created: {filePath} -> copied {destinationFolder}");
        
                        // ▲▲▲ 개선된 로그 (파일명만, 복사된 폴더경로) ▲▲▲
                        logManager.LogEvent(
                            $"[FileWatcherManager] File Created: {fileName} -> copied {destinationFolder}"
                        );
        
                        return destinationFolder;
                    }
                    catch (Exception ex)
                    {
                        logManager.LogEvent(
                            $"[FileWatcherManager] Error copying file: {fileName}. Exception: {ex.Message}"
                        );
                        if (isDebugMode)
                        {
                            logManager.LogDebug($"[FileWatcherManager] Error details: {ex.Message}");
                        }
                    }
                }
            }
        
            logManager.LogEvent($"[FileWatcherManager] No matching regex for file: {fileName}");
            return null;
        }

        private async Task<bool> WaitForFileReady(string filePath, int maxRetries = 10, int delayMilliseconds = 500)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (IsFileReady(filePath))
                {
                    return true;
                }

                // 디버그 모드일 경우 재시도 로그 기록
                if (isDebugMode)
                {
                    logManager.LogDebug($"[FileWatcherManager] Retrying access to file: {filePath}, Attempt: {attempt + 1}/{maxRetries}");
                }

                await Task.Delay(delayMilliseconds); // 대기
            }

            logManager.LogEvent($"[FileWatcherManager] File not ready after {maxRetries} retries: {filePath}");
            return false;
        }

        private bool IsDuplicateEvent(string filePath)
        {
            DateTime now = DateTime.Now;

            lock (fileProcessTracker)
            {
                if (fileProcessTracker.TryGetValue(filePath, out var lastProcessed))
                {
                    if ((now - lastProcessed).TotalMilliseconds < duplicateEventThreshold.TotalMilliseconds)
                    {
                        return true; // 중복 이벤트로 간주
                    }
                }

                fileProcessTracker[filePath] = now; // 이벤트 처리 시간 갱신
                return false;
            }
        }

        private bool IsFileReady(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true; // 파일이 준비됨
                }
            }
            catch (IOException)
            {
                return false; // 파일이 잠겨 있음
            }
        }
    }
}
