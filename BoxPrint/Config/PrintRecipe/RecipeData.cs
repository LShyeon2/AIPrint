using BoxPrint.Alarm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BoxPrint.Config
{
    public class RecipeData : ICloneable
    {
        public object Clone()
        {
            RecipeData clone = MemberwiseClone() as RecipeData;

            return clone;
        }


        /// <summary>
        /// 레시피 넘버 
        /// </summary>
        [XmlAttribute("Recipe_No")]
        public int Recipe_No { get; set; }

        /// <summary>
        /// 표시 순서
        /// </summary>
        [XmlAttribute("Order")]
        public String Order { get; set; }

        private int _iOrder;
        public int iOrder
        {
            get
            {
                int.TryParse(Order, out _iOrder);
                return _iOrder;
            }
            set
            {
                if (_iOrder != value) 
                {
                    _iOrder = value;
                    Order = _iOrder.ToString();
                }
            }
        }


        /// <summary>
        /// 데이터 타입
        /// </summary>
        [XmlAttribute("DataType")]
        public String DataType { get; set; }

        /// <summary>
        /// 설정 이름
        /// </summary>
        [XmlAttribute("Name")]
        public String Config_NM { get; set; }

        /// <summary>
        /// 설정 값
        /// </summary>
        [XmlAttribute("Value")]
        public String Config_Val { get; set; }

    }

    public class Recipe : ICloneable
    {
        public object Clone()
        {
            RecipeData clone = MemberwiseClone() as RecipeData;

            return clone;
        }


        /// <summary>
        /// 레시피 넘버 
        /// </summary>
        [XmlAttribute("Recipe_No")]
        public int Recipe_No { get; set; }

        /// <summary>
        /// 표시 순서
        /// </summary>
        [XmlAttribute("Recipe_Name")]
        public String Recipe_Name { get; set; }

        /// <summary>
        /// 적용중인 레시피
        /// </summary>
        [XmlAttribute("IsSelected")]
        public bool IsSelected { get; set; }

        //private string _Recipe_State;
        public string Recipe_State
        {
            get
            {
                return IsSelected ? "Select" : "";
            }
        }

        /// <summary>
        /// 수정일
        /// </summary>
        [XmlAttribute("ModifyDate")]
        public DateTime ModifyDate { get; set; }

        /// <summary>
        /// 생성일
        /// </summary>
        [XmlAttribute("CreateDate")]
        public DateTime CreateDate { get; set; }

    }
}
