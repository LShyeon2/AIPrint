using BoxPrint.LOCALIZATION;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace TranslationByMarkupExtension
{

    public class ResxTranslationProvider : ITranslationProvider
    {
        private readonly ResourceManager _resourceManager;

        /// <summary>
        /// 리스스 메니저 생성하는곳
        /// </summary>
        /// <param name="baseName"></param>
        /// <param name="assembly"></param>
        public ResxTranslationProvider()
        {
            _resourceManager = Resource.ResourceManager;
        }

        /// <summary>
        /// 번역 하는곳
        /// </summary>
        public object Translate(string key)
        {
            return _resourceManager.GetString(key);
        }

        /// <summary>
        /// 사용 언어 설정.. 일단은 안씀 나중에 리스트나 이런데 넣어서 쓸수있음
        /// </summary>
        public IEnumerable<CultureInfo> Languages
        {
            get
            {
                yield return new CultureInfo("ko-KR");
                yield return new CultureInfo("zh-CN");
                yield return new CultureInfo("hu-HU");
            }
        }
    }
}