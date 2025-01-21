using System.IO;
namespace FingTools.Internal
{
public static class FingHelper
{
    public static bool ValidateInteriorZipFile(string zipFilePath)
        {
            bool output = true;
            var fileName = Path.GetFileName(zipFilePath);
            if (string.IsNullOrEmpty(zipFilePath) || fileName != "moderninteriors-win.zip")
                output = false;
            return output;
        }

        public static bool ValidateExteriorZipFile(string zipFilePath)
        {
            bool output = true;
            var fileName = Path.GetFileName(zipFilePath);
            if (string.IsNullOrEmpty(zipFilePath)|| fileName != "modernexteriors-win.zip")
                output = false;
            return output;
        }

        public static bool ValidateUIZipFile(string zipFilePath)
        {
            return true;
        }
}
}