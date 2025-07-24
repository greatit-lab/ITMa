// Services\PdfMergeManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using IOPath = System.IO.Path;   // ← 이후 모든 경로 처리에 IOPath 사용
using System.Threading;    // [추가]

namespace ITM_Agent.Services
{
    public class PdfMergeManager
    {
        private readonly LogManager logManager; // 로깅 관리자
        private string _outputFolder;
        private readonly bool isDebugMode;
        private string OutputFolder
        {
            get => _outputFolder;
            set
            {
                if (!Directory.Exists(value))
                {
                    logManager.LogError($"[PdfMergeManager] Output folder does not exist: {value}");
                    throw new DirectoryNotFoundException("Output folder does not exist.");
                }
                _outputFolder = value;
            }
        }

        public PdfMergeManager(string defaultOutputFolder, LogManager logManager)
        {
            this.logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));

            if (!Directory.Exists(defaultOutputFolder))
            {
                logManager.LogError($"[PdfMergeManager] Default output folder does not exist: {defaultOutputFolder}");
                throw new DirectoryNotFoundException("Default output folder does not exist.");
            }

            _outputFolder = defaultOutputFolder;
            logManager.LogEvent($"[PdfMergeManager] Initialized with default output folder: {defaultOutputFolder}");
        }

        public void UpdateOutputFolder(string outputFolder)
        {
            OutputFolder = outputFolder;
            logManager.LogEvent($"[PdfMergeManager] Output folder updated to: {outputFolder}");
        }

        public void MergeImagesToPdf(List<string> imagePaths, string outputPdfPath)
        {
            try
            {
                if (imagePaths == null || imagePaths.Count == 0)
                {
                    logManager.LogEvent("[PdfMergeManager] No images to merge. Aborting MergeImagesToPdf().");
                    return;
                }

                // ── 1) 출력 폴더 준비 ───────────────────────────────────────
                string pdfDirectory = IOPath.GetDirectoryName(outputPdfPath);   // ← IOPath 사용
                if (string.IsNullOrEmpty(pdfDirectory))
                {
                    logManager.LogError($"[PdfMergeManager] Invalid outputPdfPath: {outputPdfPath}");
                    return;
                }
                if (!Directory.Exists(pdfDirectory))
                {
                    Directory.CreateDirectory(pdfDirectory);
                    logManager.LogEvent($"[PdfMergeManager] Created directory: {pdfDirectory}");
                }

                string fileName   = IOPath.GetFileName(outputPdfPath);          // ← IOPath 사용
                int    imageCount = imagePaths.Count;

                logManager.LogEvent($"[PdfMergeManager] Starting MergeImagesToPdf. " +
                                    $"Output: {fileName}, Images: {imageCount}");

                // ── 2) PDF 생성 ────────────────────────────────────────────
                using (var writer   = new PdfWriter(outputPdfPath))
                using (var pdfDoc   = new PdfDocument(writer))
                using (var document = new Document(pdfDoc))
                {
                    document.SetMargins(0, 0, 0, 0);

                    for (int i = 0; i < imagePaths.Count; i++)
                    {
                        string imgPath = imagePaths[i];
                        try
                        {
                            {
                            // ---------- 파일 스트림 잠김 방지 ----------
                            byte[] imgBytes = File.ReadAllBytes(imgPath);           // 추가
                            var imgData = ImageDataFactory.Create(imgBytes);    // (imgPath) 수정
                            var img = new Image(imgData);
                            float w = img.GetImageWidth(), h = img.GetImageHeight();

                            if (i > 0)
                                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

                            pdfDoc.SetDefaultPageSize(new PageSize(w, h));
                            img.SetAutoScale(false);
                            img.SetFixedPosition(0, 0);
                            img.SetWidth(w);
                            img.SetHeight(h);
                            document.Add(img);

                            if (isDebugMode)
                                logManager.LogDebug($"[PdfMergeManager] Added page {i + 1}: {imgPath} ({w}x{h})");
                        }
                        catch (Exception exImg)
                        {
                            logManager.LogError($"[PdfMergeManager] Error adding image '{imgPath}': {exImg.Message}");
                        }
                    }
                    document.Close();   // 반드시 Close 호출
                }

                // ── 3) 이미지 파일 삭제 ────────────────────────────────────
                int delOk = 0, delFail = 0;
                foreach (string imgPath in imagePaths)
                {
                    if (DeleteFileWithRetry(imgPath, maxRetry: 5, delayMs: 300))   // [수정]
                    {
                        delOk++;
                        if (isDebugMode)
                            logManager.LogDebug($"[PdfMergeManager] Deleted image file: {imgPath}");
                    }
                    else
                    {
                        delFail++;
                    }
                }
        
                logManager.LogEvent($"[PdfMergeManager] Merge completed. " +
                                    $"Images merged: {imagePaths.Count}, deleted: {delOk}, delete-failed: {delFail}");
            }
            catch (Exception ex)
            {
                logManager.LogError($"[PdfMergeManager] MergeImagesToPdf failed. Exception: {ex.Message}");
                throw;  // 상위 호출부로 재전달
            }
        }
        
        /* ===================== [추가] 헬퍼 메서드 ===================== */
        /// <summary>
        /// 파일 잠김 문제를 고려해 삭제를 재시도한다.
        /// </summary>
        private bool DeleteFileWithRetry(string filePath, int maxRetry = 5, int delayMs = 300)
        {
            for (int attempt = 1; attempt <= maxRetry; attempt++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal); // Read-only 해제
                        File.Delete(filePath);
                    }
                    return true;  // 성공
                }
                catch (IOException) when (attempt < maxRetry)
                {
                    Thread.Sleep(delayMs);   // 잠시 대기 후 재시도
                }
                catch (Exception ex)
                {
                    logManager.LogError($"[PdfMergeManager] Delete retry {attempt} failed: {ex.Message}");
                    if (attempt >= maxRetry) return false;
                    Thread.Sleep(delayMs);
                }
            }
            return false;
        }
    }
}
