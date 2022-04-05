using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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

            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите группу объектов"); //выбираем объект как ссылку
            Element element = doc.GetElement(reference); //по ссылке добираемся до элемента
            Group group = element as Group; //т.к. мы работаем именн с группой, то преобразовываем из класса Element в группу (чтобы иметь доступ ко всем операциям с ней)
            //преобразование as предпочтительнее чем (Group), т.к. не даст исключение, а вернет значение null, если преобразование не удастся

            XYZ point = uiDoc.Selection.PickPoint("Выберите точку для вставки"); //даем пользователю выбрать точку в модели

            //так как вставка осуществляется через изменение модели, то необходимо использовать транзакцию
            Transaction transaction = new Transaction(doc);
            transaction.Start("Копирование группы объектов");
            doc.Create.PlaceGroup(point, group.GroupType);
            transaction.Commit();

            return Result.Succeeded;
        }
    }
}
