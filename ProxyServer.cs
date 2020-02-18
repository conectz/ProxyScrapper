using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyScrapper
{
    public class ProxyServer
    {
        public string serverAddress { get; set; }
        public int port { get; set; }
        
        public bool active { get; set; }
    }
}
