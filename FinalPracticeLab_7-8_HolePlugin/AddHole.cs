using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalPracticeLab_7_8_HolePlugin
{
    [Transaction(TransactionMode.Manual)]
    public class AddHole : IExternalCommand

    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region Поиск файлов и элементов для работы
            Document arDoc = commandData.Application.ActiveUIDocument.Document;//Основной файл АР. Только в основном файле можно выполнять транзакции
            Document ovDoc = arDoc.Application.Documents.OfType<Document>().Where(x => x.Title.Contains("ОВ")).FirstOrDefault();//Связанный файл ОВ. В нем можно выполнять все, кроме транзакций. Можно искать не по Title, а по PathName
            if (ovDoc == null)//проверка, найден ли связанный файл ОВ
            {
                TaskDialog.Show("Ошибка", "Не найден ОВ файл");
                return Result.Cancelled;
            }

            FamilySymbol familySymbol = new FilteredElementCollector(arDoc)//ищем семейство отверстий в проекте
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_GenericModel)//По умолчанию наше новое семейство попало в категорнию обобщенные
                    .OfType<FamilySymbol>()
                    .Where(x => x.FamilyName.Equals("Отверстие_с_ Учебы"))
                    .FirstOrDefault();

            if (familySymbol == null)//проверка, заггружено ли семейство отверстий в проект
            {
                TaskDialog.Show("Ошибка", "Не найдено семейство отверстий. Загрузите семейство в проект");
                return Result.Cancelled;
            }

            List<Duct> ducts = new FilteredElementCollector(ovDoc)//Ищем все воздуховоды
                    .OfClass(typeof(Duct))
                    .OfType<Duct>()
                    .ToList();

            List<Pipe> pipes = new FilteredElementCollector(ovDoc)//Ищем все трубы
                    .OfClass(typeof(Pipe))
                    .OfType<Pipe>()
                    .ToList();

            View3D view3D = new FilteredElementCollector(arDoc)//находим 3Д вид
                    .OfClass(typeof(View3D))
                    .OfType<View3D>()
                    .Where(x => !x.IsTemplate)//Отсеиваем шаблоны видов, проверкой не установлено ли свойство IsTemplate
                    .FirstOrDefault();

            if (view3D == null)//проверка, найден ли 3Д вид
            {
                TaskDialog.Show("Ошибка", "Не найден 3D вид");
                return Result.Cancelled;
            }
            #endregion


            ReferenceIntersector referenceIntersector = new ReferenceIntersector(new ElementClassFilter(typeof(Wall)), FindReferenceTarget.Element, view3D);//создаем объект, содержащий все перечения со стенами, в котором будем искать перечения

            Transaction transaction = new Transaction(arDoc);
            transaction.Start("Расстановка отверстий");

            #region Расстановка отверстий на перечении с воздуховодами
            foreach (Duct d in ducts)
            {
                Line curve = (d.Location as LocationCurve).Curve as Line;//Приводим Curve к Line, т.к. у Curve нет свойства Direction, а оно нам потребуется далее
                XYZ point = curve.GetEndPoint(0);//точка, из которой выходит луч
                XYZ direction = curve.Direction;//направление луча

                List<ReferenceWithContext> intersections = referenceIntersector.Find(point, direction)//класс ReferenceWithContext может нам дыть ссылку на объект
                .Where(x => x.Proximity <= curve.Length)// Proximity - расстояние
                .Distinct(new ReferenceWithContextElementEqualityComparer())//отфильтровываем повторения перечений с двумя плоскостями стены с помощью созданного класса
                .ToList();//получили набор пересчений

                foreach (ReferenceWithContext intersection in intersections)
                {
                    double proximity = intersection.Proximity;//расстояние до стены
                    Reference reference = intersection.GetReference();//ссылка на стену
                    Wall wall = arDoc.GetElement(reference.ElementId) as Wall;//сама стена
                    Level level = arDoc.GetElement(wall.LevelId) as Level;//находим уровень стены для вставки отверстия
                    XYZ insertPoint = point + (direction * proximity);//точка вставки отверстия

                    FamilyInstance hole = arDoc.Create.NewFamilyInstance(insertPoint, familySymbol, wall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    hole.LookupParameter("Ширина").Set(d.Diameter);//устанавливаем параметры габаритов вставляемого отверстия, равным диаметрам воздуховодов
                    hole.LookupParameter("Высота").Set(d.Diameter);
                }
            }
            #endregion

            #region Расстановка отверстий на перечении с трубами
            foreach (Pipe p in pipes)
            {
                Line curve = (p.Location as LocationCurve).Curve as Line;//Приводим Curve к Line, т.к. у Curve нет свойства Direction, а оно нам потребуется далее
                XYZ point = curve.GetEndPoint(0);//точка, из которой выходит луч
                XYZ direction = curve.Direction;//направление луча

                List<ReferenceWithContext> intersections = referenceIntersector.Find(point, direction)//класс ReferenceWithContext может нам дыть ссылку на объект
                .Where(x => x.Proximity <= curve.Length)// Proximity - расстояние
                .Distinct(new ReferenceWithContextElementEqualityComparer())//отфильтровываем повторения перечений с двумя плоскостями стены с помощью созданного класса
                .ToList();//получили набор пересчений

                foreach (ReferenceWithContext intersection in intersections)
                {
                    double proximity = intersection.Proximity;//расстояние до стены
                    Reference reference = intersection.GetReference();//ссылка на стену
                    Wall wall = arDoc.GetElement(reference.ElementId) as Wall;//сама стена
                    Level level = arDoc.GetElement(wall.LevelId) as Level;//находим уровень стены для вставки отверстия
                    XYZ insertPoint = point + (direction * proximity);//точка вставки отверстия

                    FamilyInstance hole = arDoc.Create.NewFamilyInstance(insertPoint, familySymbol, wall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    hole.LookupParameter("Ширина").Set(p.Diameter);//устанавливаем параметры габаритов вставляемого отверстия, равным диаметрам воздуховодов
                    hole.LookupParameter("Высота").Set(p.Diameter);
                }
            }
            #endregion

            transaction.Commit();
            return Result.Succeeded;
        }
    }
}
