using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSService
{
    public class BaseRSService
    {
        protected static string _data_folder = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, "data");
        protected static void WriteFile(string file_name, string content, bool append = true)
        {
            if (!string.IsNullOrWhiteSpace(file_name))
            {
                string full_path = Path.Combine(_data_folder, file_name);
                if (append)
                {
                    File.AppendAllText(full_path, content);
                }
                else
                {

                    File.WriteAllText(full_path, content);
                }
            }
        }

        protected static string[] ReadFileAsLine(string file_name)
        {
            string full_path = Path.Combine(_data_folder, file_name);
            if (File.Exists(full_path))
            {
                return File.ReadAllLines(full_path);
            }
            return new string[] { };
        }

        protected static string ReadFile(string file_name)
        {
            string full_path = Path.Combine(_data_folder, file_name);
            if (File.Exists(full_path))
            {
                return File.ReadAllText(full_path);
            }
            return string.Empty;
        }
    }
}
