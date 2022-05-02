using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalPracticeLab_7_8_HolePlugin
{
    //Класс взять из примера на сайте: https://adn-cis.org/forum/index.php?topic=2872.15
    public class ReferenceWithContextElementEqualityComparer : IEqualityComparer<ReferenceWithContext>
    {
        public bool Equals(ReferenceWithContext x, ReferenceWithContext y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(null, x)) return false;
            if (ReferenceEquals(null, y)) return false;

            var xReference = x.GetReference();

            var yReference = y.GetReference();

            return xReference.LinkedElementId == yReference.LinkedElementId  //сравнивают ID элементов из связанного файла. Они также должны быть одинаковы
                       && xReference.ElementId == yReference.ElementId;//сравнивают ID элементов из основного файла
        }

        public int GetHashCode(ReferenceWithContext obj)
        {
            var reference = obj.GetReference();

            unchecked
            {
                return (reference.LinkedElementId.GetHashCode() * 397) ^ reference.ElementId.GetHashCode();
            }
        }
    }
}
