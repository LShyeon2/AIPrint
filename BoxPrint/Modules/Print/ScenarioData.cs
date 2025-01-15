using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BoxPrint.Config.Print
{
    public class ScenarioData : ICloneable
    {
        public object Clone()
        {
            RecipeData clone = MemberwiseClone() as RecipeData;

            return clone;
        }


        /// <summary>
        /// 순서
        /// </summary>
        [XmlAttribute("Step")]
        public String Step { get; set; }

        private int _iStep;
        public int iStep
        {
            get
            {
                int.TryParse(Step, out _iStep);
                return _iStep;
            }
            set
            {
                if (_iStep != value)
                {
                    _iStep = value;
                    Step = _iStep.ToString();
                }
            }
        }

        /// <summary>
        /// 레시피 넘버 
        /// </summary>
        [XmlAttribute("Recipe_No")]
        public int Recipe_No { get; set; }


        /// <summary>
        /// 레시피 이름
        /// </summary>
        [XmlAttribute("Recipe_Name")]
        public String Recipe_Name { get; set; }


    }
}
