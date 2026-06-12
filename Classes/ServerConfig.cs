using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FemRec2023.Classes
{
    public class ServerConfig
    {
        public static object Bracket => new List<object>(); 
        public static string BaseURL = "https://logan2021.obfacus.com"; // replace with your url
        public static int GameVersion = 20230406;
    }
}
