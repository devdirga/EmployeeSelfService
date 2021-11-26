using System;

namespace KANO.Core.Lib.Extension
{
    public static class StringNormExt
    {
        public static string NormalizeSearch(this string str)
        {
            if (str == null)
                return null;

            return str.Normalize(System.Text.NormalizationForm.FormKD).ToLowerInvariant();
        }
    }
}
