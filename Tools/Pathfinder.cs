using System;
using System.IO;

namespace VRise.Tools
{
    public class Pathfinder
    {
        public static string mainFolder
        {
            get
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }
    }
}
