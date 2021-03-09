using System.Collections.Generic;

namespace Balu0._1.Models
{
    public class OrdersColorsViewModel
    {
        public Programa order { get; set; }
        public List<ColorsAndQuantities> colers { get; set; }
        public Malha malha { get; set; }
        public Preco_Malha preco { get; set; }
        public List<Programa_Malha> promalha { get; set; }

        public Programa_Cor_Info_Status statusses { get; set; }
        public Programa_Cor_Info Quantpec { get; set; }
    }
}
