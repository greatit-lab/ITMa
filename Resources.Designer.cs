// Resources.Designer.cs
namespace ITM_Agent.Propertiesa {
    using System;
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   이 클래스에서 사용하는 캐시된 ResourceManager 인스턴스를 반환합니다.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ITM_Agent.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   이 강력한 형식의 리소스 클래스를 사용하여 모든 리소스 조회에 대해 현재 스레드의 CurrentUICulture 속성을
        ///   재정의합니다.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   경고과(와) 유사한 지역화된 문자열을 찾습니다.
        /// </summary>
        internal static string CAPTION_WARNING {
            get {
                return ResourceManager.GetString("CAPTION_WARNING", resourceCulture);
            }
        }
        
        /// <summary>
        ///   폴더가 미선택되었습니다.과(와) 유사한 지역화된 문자열을 찾습니다.
        /// </summary>
        internal static string MSG_BASE_NOT_SELECTED {
            get {
                return ResourceManager.GetString("MSG_BASE_NOT_SELECTED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   복사 폴더를 선택해주세요.과(와) 유사한 지역화된 문자열을 찾습니다.
        /// </summary>
        internal static string MSG_FOLDER_REQUIRED {
            get {
                return ResourceManager.GetString("MSG_FOLDER_REQUIRED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   정규표현식을 입력해주세요.과(와) 유사한 지역화된 문자열을 찾습니다.
        /// </summary>
        internal static string MSG_REGEX_REQUIRED {
            get {
                return ResourceManager.GetString("MSG_REGEX_REQUIRED", resourceCulture);
            }
        }
    }
}
