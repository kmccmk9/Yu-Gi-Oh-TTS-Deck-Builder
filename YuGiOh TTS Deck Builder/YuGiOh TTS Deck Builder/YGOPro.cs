using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YuGiOh_TTS_Deck_Builder
{
    public class YGOProCardDetails
    {
        public int id { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public object atk { get; set; }
        public object def { get; set; }
        public object scale { get; set; }
        public string type { get; set; }
        public object level { get; set; }
        public string race { get; set; }
        public object attribute { get; set; }
        public object linkval { get; set; }
        public object linkmarkers { get; set; }
        public int staple { get; set; }
        public int upcount { get; set; }
        public int downcount { get; set; }
        public int times { get; set; }
        public int timesperweek { get; set; }
        public object ban_tcg { get; set; }
        public object ban_ocg { get; set; }
        public object ban_goat { get; set; }
        public object format { get; set; }
        public string archetype { get; set; }
        public object prename { get; set; }
        public string tcg_date { get; set; }
        public object ocg_date { get; set; }
        public string currentPrice_cm { get; set; }
        public string currentPrice_tcg { get; set; }
        public string imgsize { get; set; }
        public double currentPrice_cool { get; set; }
        public string currentPrice_cool_url { get; set; }
    }
}
