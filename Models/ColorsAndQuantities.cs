using System.Collections.Generic;

namespace Balu0._1.Models
{
    public class ColorsAndQuantities
    {
        public Programa_Cor coler { get; set; }
        public List<Programa_Cor_Info> quant { get; set; }
        public List<Programa_Cor_Info_Status> status { get; set; }

    }
}


