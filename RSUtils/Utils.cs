using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace RSUtils
{
    public class RSUtils
    {
        private const string keyDefault = "4a53afd6f286e316ad3d9f50579ec8fe";

        public static long TimeInEpoch(DateTime? dt = null) => dt.HasValue ? (long)(dt.Value.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds : DateTimeOffset.Now.ToUnixTimeSeconds();

        public static long TimeInEpochMS(DateTime? dt = null) => dt.HasValue ? (long)(dt.Value.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalMilliseconds : DateTimeOffset.Now.ToUnixTimeMilliseconds();

        public static DateTimeOffset EpochToTime(long ep)
        {
            try
            {
                return ep < 9999999999L ? DateTimeOffset.FromUnixTimeSeconds(ep).ToLocalTime() : DateTimeOffset.FromUnixTimeMilliseconds(ep).ToLocalTime();
            }
            catch
            {
            }
            return (DateTimeOffset)DateTime.Now.ToLocalTime();
        }

        public static string EpochToTimeStringShortFomart(long ep) => RSUtils.EpochToTime(ep).ToString("dd/MM/yyyy HH:mm");

        public static string EpochToTimeString(long ep, string format = "dd/MM/yyyy HH:mm:ss") => ep <= 0L ? "" : RSUtils.EpochToTime(ep).ToString(format);

        public static IEnumerable<Tuple<string, string, string>> GetHREFFromHtml(string content)
        {
            Regex r = new Regex("<a.*?href=(\"|')(?<href>.*?)(\"|').*?>(?<value>.*?)</a>");
            foreach(Match match in r.Matches(content))
                yield return new Tuple<string, string, string>(match.Groups["href"].Value, match.Groups["value"].Value, match.Groups[0].Value);
        }

        public static string Encode_UTF8(string input)
        {
            try
            {
                string md5Hex = RSUtils.ConfigurationManager.AppSetting["md5key"];
                return RSUtils.Encrypt_UTF8(input, md5Hex);
            }
            catch
            {
            }
            return "";
        }

        public static string Decode_UTF8(string token)
        {
            try
            {
                string hex = RSUtils.ConfigurationManager.AppSetting["md5key"];
                return RSUtils.Decrypt_UTF8(token, hex);
            }
            catch
            {
            }
            return "";
        }

        public static string Encrypt_UTF8(string text, string md5Hex)
        {
            if(string.IsNullOrWhiteSpace(md5Hex))
                md5Hex = "4a53afd6f286e316ad3d9f50579ec8fe";
            byte[] bytes1 = RSUtils.String_To_Bytes(md5Hex);
            TripleDESCryptoServiceProvider cryptoServiceProvider = new TripleDESCryptoServiceProvider();
            cryptoServiceProvider.IV = new byte[8];
            cryptoServiceProvider.Key = bytes1;
            byte[] bytes2 = Encoding.UTF8.GetBytes(text);
            ICryptoTransform encryptor = cryptoServiceProvider.CreateEncryptor();
            using(MemoryStream memoryStream = new MemoryStream())
            {
                using(CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes2, 0, bytes2.Length);
                    cryptoStream.FlushFinalBlock();
                }
                return RSUtils.Bytes_To_String(memoryStream.ToArray());
            }
        }

        public static string Decrypt_UTF8(string input, string hex)
        {
            if(string.IsNullOrWhiteSpace(input))
                return "";
            if(string.IsNullOrWhiteSpace(hex))
                hex = "4a53afd6f286e316ad3d9f50579ec8fe";
            byte[] bytes1 = RSUtils.String_To_Bytes(hex);
            byte[] bytes2 = RSUtils.String_To_Bytes(input);
            TripleDESCryptoServiceProvider cryptoServiceProvider = new TripleDESCryptoServiceProvider();
            cryptoServiceProvider.IV = new byte[8];
            cryptoServiceProvider.Key = bytes1;
            ICryptoTransform decryptor = cryptoServiceProvider.CreateDecryptor();
            using(MemoryStream memoryStream = new MemoryStream())
            {
                using(CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes2, 0, bytes2.Length);
                    cryptoStream.FlushFinalBlock();
                }
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        public static string Encode(string input)
        {
            try
            {
                string md5Hex = RSUtils.ConfigurationManager.AppSetting["md5key"];
                return RSUtils.Encrypt(input, md5Hex);
            }
            catch
            {
            }
            return "";
        }

        public static string DecodeToken(string token)
        {
            try
            {
                string hex = RSUtils.ConfigurationManager.AppSetting["md5key"];
                return RSUtils.Decrypt(token, hex);
            }
            catch
            {
            }
            return "";
        }

        public static string Encrypt(string text, string md5Hex)
        {
            if(string.IsNullOrEmpty(md5Hex))
                md5Hex = "4a53afd6f286e316ad3d9f50579ec8fe";
            byte[] bytes1 = RSUtils.String_To_Bytes(md5Hex);
            TripleDESCryptoServiceProvider cryptoServiceProvider = new TripleDESCryptoServiceProvider();
            cryptoServiceProvider.IV = new byte[8];
            cryptoServiceProvider.Key = bytes1;
            byte[] bytes2 = Encoding.ASCII.GetBytes(text);
            ICryptoTransform encryptor = cryptoServiceProvider.CreateEncryptor();
            using(MemoryStream memoryStream = new MemoryStream())
            {
                using(CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes2, 0, bytes2.Length);
                    cryptoStream.FlushFinalBlock();
                }
                return RSUtils.Bytes_To_String(memoryStream.ToArray());
            }
        }

        public static string Encrypt(string text, byte[] hash)
        {
            TripleDESCryptoServiceProvider cryptoServiceProvider = new TripleDESCryptoServiceProvider();
            cryptoServiceProvider.IV = new byte[8];
            cryptoServiceProvider.Key = hash;
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            ICryptoTransform encryptor = cryptoServiceProvider.CreateEncryptor();
            using(MemoryStream memoryStream = new MemoryStream())
            {
                using(CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes, 0, bytes.Length);
                    cryptoStream.FlushFinalBlock();
                }
                return RSUtils.Bytes_To_String(memoryStream.ToArray());
            }
        }

        public static string ByteArrayToString(byte[] encryptedBytes)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for(int index = 0; index < encryptedBytes.Length; ++index)
                stringBuilder.Append(encryptedBytes[index].ToString("x2"));
            return stringBuilder.ToString();
        }

        public static string Decrypt(string input, byte[] hash)
        {
            byte[] bytes = RSUtils.String_To_Bytes(input);
            TripleDESCryptoServiceProvider cryptoServiceProvider = new TripleDESCryptoServiceProvider();
            cryptoServiceProvider.IV = new byte[8];
            cryptoServiceProvider.Key = hash;
            ICryptoTransform decryptor = cryptoServiceProvider.CreateDecryptor();
            using(MemoryStream memoryStream = new MemoryStream())
            {
                using(CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes, 0, bytes.Length);
                    cryptoStream.FlushFinalBlock();
                }
                return Encoding.ASCII.GetString(memoryStream.ToArray());
            }
        }

        public static string Decrypt(string input, string hex)
        {
            if(string.IsNullOrEmpty(input))
                return "";
            if(string.IsNullOrEmpty(hex))
                hex = "4a53afd6f286e316ad3d9f50579ec8fe";
            byte[] bytes1 = RSUtils.String_To_Bytes(hex);
            byte[] bytes2 = RSUtils.String_To_Bytes(input);
            TripleDESCryptoServiceProvider cryptoServiceProvider = new TripleDESCryptoServiceProvider();
            cryptoServiceProvider.IV = new byte[8];
            cryptoServiceProvider.Key = bytes1;
            ICryptoTransform decryptor = cryptoServiceProvider.CreateDecryptor();
            using(MemoryStream memoryStream = new MemoryStream())
            {
                using(CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes2, 0, bytes2.Length);
                    cryptoStream.FlushFinalBlock();
                }
                return Encoding.ASCII.GetString(memoryStream.ToArray());
            }
        }

        private static byte[] String_To_Bytes(string strInput)
        {
            int startIndex = 0;
            int index = 0;
            byte[] bytes = new byte[strInput.Length / 2];
            while(strInput.Length > startIndex + 1)
            {
                long int32 = (long)Convert.ToInt32(strInput.Substring(startIndex, 2), 16);
                bytes[index] = Convert.ToByte(int32);
                startIndex += 2;
                ++index;
            }
            return bytes;
        }

        public static string GetHexMD5(string input)
        {
            byte[] hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(input));
            StringBuilder stringBuilder = new StringBuilder();
            for(int index = 0; index < hash.Length; ++index)
                stringBuilder.Append(hash[index].ToString("x2"));
            return stringBuilder.ToString();
        }

        public static byte[] GetBytesFromString(string str)
        {
            byte[] dst = new byte[str.Length * 2];
            Buffer.BlockCopy((Array)str.ToCharArray(), 0, (Array)dst, 0, dst.Length);
            return dst;
        }

        public static string GetStringFromByteArr(byte[] bytes)
        {
            char[] dst = new char[bytes.Length / 2];
            Buffer.BlockCopy((Array)bytes, 0, (Array)dst, 0, bytes.Length);
            return new string(dst);
        }

        public static string GetHexMD5(string input, bool isUtf8)
        {
            byte[] hash = MD5.Create().ComputeHash(!isUtf8 ? Encoding.ASCII.GetBytes(input) : Encoding.UTF8.GetBytes(input));
            StringBuilder stringBuilder = new StringBuilder();
            for(int index = 0; index < hash.Length; ++index)
                stringBuilder.Append(hash[index].ToString("x2"));
            return stringBuilder.ToString();
        }

        public static byte[] GetHashMD5(string input) => MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(input));

        private static string Bytes_To_String(byte[] bytes_Input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for(int index = 0; index <= bytes_Input.GetUpperBound(0); ++index)
            {
                int num = int.Parse(bytes_Input[index].ToString());
                stringBuilder.Append(num.ToString("X").PadLeft(2, '0'));
            }
            return stringBuilder.ToString();
        }

        public static string GenSKU(string input)
        {
            input = RSUtils.TiengVietKhongDau(input);
            return Regex.Replace(string.Join("-", input.ToLower().Split(' ')), "(-)\\1{1,}", "$1");
        }

        public static string TiengVietKhongDau(string input)
        {
            string[] strArray1 = new string[8];
            string[] strArray2 = new string[8]
            {
        "a",
        "e",
        "o",
        "u",
        "i",
        "y",
        "d",
        " "
            };
            strArray1[0] = "àảáạãâầấẩậẫăằẳắặẵ";
            strArray1[1] = "ềếệễểêẹéèẻẽ";
            strArray1[2] = "òỏóọõôồổốộỗơớợởờỡ";
            strArray1[3] = "úùủụừứựửữưũ";
            strArray1[4] = "ịỉìĩí";
            strArray1[5] = "ỷýỵỳỹ";
            strArray1[6] = "đ";
            strArray1[7] = "^a-z0-9";
            for(int index = 0; index < 8; ++index)
                input = Regex.Replace(input, "[" + strArray1[index] + "]", strArray2[index], RegexOptions.IgnoreCase);
            return RSUtils.StringOptimal(input);
        }

        public static string StringOptimal(string input)
        {
            try
            {
                input = Regex.Replace(input, "[\\t\\f]| ", " ", RegexOptions.Singleline, TimeSpan.FromSeconds(5.0));
                input = Regex.Replace(input, "([\\s\\t\\f]*[\\r\\n][\\s\\t\\f]*){2,}", "\n", RegexOptions.Singleline, TimeSpan.FromSeconds(5.0));
                input = Regex.Replace(input, "[ ]{2,}", " ", RegexOptions.Singleline, TimeSpan.FromSeconds(5.0));
                input = input.TrimStart().TrimEnd();
            }
            catch
            {
            }
            return input;
        }

        public static string GetToken(string userIP)
        {
            try
            {
                if(userIP == "::1")
                    userIP = "127.0.0.1";
                return RSUtils.Encode(string.Format("{0}-{1}", RSUtils.UnixTime(new DateTime?(DateTime.Now)),userIP));
            }
            catch
            {
                return "";
            }
        }

        public static long UnixTime(DateTime? date = null)
        {
            try
            {
                if(!date.HasValue)
                    date = new DateTime?(DateTime.Now);
                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return Convert.ToInt64((date.GetValueOrDefault().ToUniversalTime() - dateTime).TotalSeconds);
            }
            catch(Exception ex)
            {
                return 0;
            }
        }

        public static class ConfigurationManager
        {
            public static IConfiguration AppSetting { get; } = (IConfiguration)new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json").Build();
        }
    }
}
