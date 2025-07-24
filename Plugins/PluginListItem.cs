// Plugins\PluginListItem.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITM_Agent.Plugins
{
    public class PluginListItem
    {
        public string PluginName { get; set; }
        public string AssemblyPath { get; set; }
        public string PluginVersion { get; set; }    // ★ 추가: 어셈블리 버전 저장

        // ☆ 변경: 이름만 리턴하던 ToString()을 버전 포함 형태로 수정
        public override string ToString()
        {
            return string.IsNullOrEmpty(PluginVersion)
                ? PluginName
                : $"{PluginName} (v{PluginVersion})";
        }
    }
}
