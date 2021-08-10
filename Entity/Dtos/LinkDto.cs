using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Dtos
{
    public class LinkDto
    {
        public string Href { get; set; }

        public  string Rel  { get; set; }
        public string Method  { get; set; }
        public LinkDto(string href,string rel, string method)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }
    }
}
