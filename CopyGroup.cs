using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document; //получаем доступ к интерфейсу активного документа, чтобы выбрать объект в нем

            try
            {
                GroupPickFilter groupPickFilter = new GroupPickFilter(); //создаем экземпляр класса фильтра выбора 
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов"); //выбираем объект как ссылку
                //добавили в выбор фильтр, чтобы можно было выбрать курсором только группу и никакие другие объекты
                
                Element element = doc.GetElement(reference); //по ссылке добираемся до элемента
                Group group = element as Group; //т.к. мы работаем именн с группой, то преобразовываем из класса Element в группу (чтобы иметь доступ ко всем операциям с ней)
                                                //преобразование as предпочтительнее чем (Group), т.к. не даст исключение, а вернет значение null, если преобразование не удастся

                XYZ groupCenter = GetElementCenter(group); //вычисляем центр координат группы с использованием созданного ниже метода
                Room room = GetRoomByPoint(doc, groupCenter); //находим какой комнате принадлежит группа в которой находится выбираемая группа с помощью созданного ниже метода
                XYZ roomCenter = GetElementCenter(room); //находим центр координат этой комнаты тем же методом, что и центр группы
                XYZ offset = groupCenter - roomCenter; //находим смещение координат центра группы относительно центра комнаты


                XYZ pickedPoint = uiDoc.Selection.PickPoint("Выберите точку для вставки"); //даем пользователю выбрать точку в модели
                Room roomToInsert = GetRoomByPoint(doc, pickedPoint); //находим какой комнате принадлежит точка в которую ткнул пользователь
                XYZ roomToInsertCenter = GetElementCenter(roomToInsert); //вычисляем центр этой комнаты
                XYZ pointToInsert = roomToInsertCenter + offset; //находим точку вставки с учетом смещения координат центра группы в исходной комнате

                //так как вставка осуществляется через изменение модели, то необходимо использовать транзакцию
                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(pointToInsert, group.GroupType);
                transaction.Commit();
            }
            catch(Autodesk.Revit.Exceptions.OperationCanceledException) //иключение при нажатии клавиши ESC
            {
                return Result.Cancelled;
            }
            catch(Exception ex) //обработка любых других исключений с выводом причины
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element) //создаем метод для вычисления центра элемента (в нашем случае группы и потом помещения), возвращает координаты
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null); //получаем координаты BoundingBox (рамки), ограничивающей объект
            return (bounding.Max + bounding.Min) / 2; //вычисляем середину между ближней левой и крайней правой точкой рамки, ограничивающей объект
        }

        public Room GetRoomByPoint(Document doc, XYZ point) //создаем метод для определения имени комнаты по точке, уточняя, что фильтовать по нашему документу
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms); //отбираем в документе все комнаты в фильтр-коллектор
            foreach (Element el in collector)
            {
                Room room = el as Room; //выполняем преобразование элемента к типу комната
                if (room != null) //проверяем удалось ли приведение строкой выше
                {
                    if (room.IsPointInRoom(point)) //с помощью специального свойства проверяем находится ли точка в комнате
                    {
                        return room; // если да, то возвращаем имя комнаты
                    }
                }
            }
            return null; //это если мы вообще не найдем по каким-то причинам какой комнате принадлежит точка
        }
    }

    public class GroupPickFilter : ISelectionFilter //создаем класс для фильтра по типу объекта
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups) //вот он фильтр, что объект именно ГРУППА
                return true;
            else
                return false;
            
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false; //т.к. нам не интересна ссылка в качестве фильтра объектов
        }
    }
}
