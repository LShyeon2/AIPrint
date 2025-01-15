using System.Collections.Generic;
using System.Globalization;

namespace TranslationByMarkupExtension
{
    public interface ITranslationProvider
    {
        /// <summary>
        /// 키로 번역
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object Translate(string key);

        /// <summary>
        /// 언어 설정 하는곳 일단은 안씀
        /// </summary>
        IEnumerable<CultureInfo> Languages { get; }

    }
}
