using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPI_WPFCreateDuctFromPoint
{
    class MainViewViewModel
    {
        private ExternalCommandData _commandData;

        //Список систем
        public List<MEPSystemType> MepSystemTypeS { get; set; } = new List<MEPSystemType>();
        public MEPSystemType SelectedSystemType { get; set; }

        //Список всех типов воздуховодов
        public List<DuctType> DuctsTypes { get; private set; } = new List<DuctType>();
        public DuctType SelectedDuctsType { get; set; }

        public double DuctIndent { get; set; }


        //Список уровней
        public List<Level> Levels { get; private set; } = new List<Level>();
        public Level SelectedLevel { get; set; }


        public DelegateCommand CreateDuct { get; private set; }

        public List<XYZ> Points { get; private set; } = new List<XYZ>();


        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;

            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //Находим все типы воздуховодов
            DuctsTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(DuctType))
                .Cast<DuctType>()
                .ToList();

            //Находим все уровни
            Levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            //Находим все системы
            MepSystemTypeS = new FilteredElementCollector(doc)
           .OfClass(typeof(MEPSystemType))
           .Cast<MEPSystemType>()
           .Where(x => x is MechanicalSystemType)
           .ToList();

            CreateDuct = new DelegateCommand(OnCreateDuct);

            //Начальное значение отступа
            DuctIndent = 0;

            //Выбор точек
            Points = GetPoints(_commandData, "Выберите точки", ObjectSnapTypes.Endpoints,2);
        }

        private void OnCreateDuct()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (Points.Count < 2 ||
                SelectedDuctsType == null ||
                SelectedLevel == null ||
                SelectedSystemType == null)
                return;

            using (var trans = new Transaction(doc, "Создаём воздуховоды"))
            {
                trans.Start();

                for (int i = 1; i < Points.Count; i++)
                {
                    //(Document document, ElementId systemTypeId, ElementId ductTypeId, ElementId levelId, XYZ startPoint, XYZ endPoint)
                    var duct=  Duct.Create(doc, SelectedSystemType.Id, SelectedDuctsType.Id, SelectedLevel.Id, Points[i - 1], Points[i]);
                    duct.LevelOffset = UnitUtils.ConvertToInternalUnits(DuctIndent, UnitTypeId.Millimeters);
                }                
                trans.Commit();
            }            
            RaiseCloseRequest();
        }

        public event EventHandler CloseRequest;

        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }

        public static List<XYZ> GetPoints(ExternalCommandData commandData,
                            string promptMessage, ObjectSnapTypes objectSnapTypes, int countPoints)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            List<XYZ> points = new List<XYZ>();

            for(int i =0; i<countPoints; i++)
            {
                XYZ pickedPoint = null;
                pickedPoint = uidoc.Selection.PickPoint(objectSnapTypes, promptMessage);

                points.Add(pickedPoint);
            }
            return points;
        }

    }
}
