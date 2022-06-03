using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPI_WPFElementBetweenPoints
{
    class MainViewViewModel
    {
        private ExternalCommandData _commandData;

        //Список всех типов 
        public List<FamilySymbol> ElementTypes { get; private set; } = new List<FamilySymbol>();
        public FamilySymbol SelectedElementType { get; set; }

        //Список уровней
        public List<Level> Levels { get; private set; } = new List<Level>();
        public Level SelectedLevel { get; set; }

        public int ElementCount { get; set; }

        public DelegateCommand AddElements { get; private set; }

        public List<XYZ> InsertPoints { get; private set; } = new List<XYZ>();


        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;

            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //Находим все типы мебели
            ElementTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .ToList();

            //Находим все уровни
            Levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();


            AddElements = new DelegateCommand(OnAddElements);

            //Выбор точек
            InsertPoints = GetPoints(_commandData, "Выберите точки", ObjectSnapTypes.Endpoints, 2);
        }

        private void OnAddElements()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (SelectedElementType == null ||
                SelectedLevel == null ||
                ElementCount < 1)
                return;

            using (var trans = new Transaction(doc, "добавляем мебель"))
            {
                trans.Start();

                if (!SelectedElementType.IsActive)
                    SelectedElementType.Activate();

                XYZ lenPoint = InsertPoints[1] - InsertPoints[0];
                XYZ stepPoint = lenPoint / (ElementCount+1);

                for (int i = 1; i <= ElementCount; i++)
                {
                    XYZ InsertPoint = InsertPoints[0] + stepPoint * i;
                    //NewFamilyInstance(XYZ location, DB.FamilySymbol symbol, Level level, StructuralType structuralType);
                    doc.Create.NewFamilyInstance(InsertPoint, SelectedElementType, SelectedLevel, StructuralType.NonStructural);
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

            for (int i = 0; i < countPoints; i++)
            {
                XYZ pickedPoint = null;
                pickedPoint = uidoc.Selection.PickPoint(objectSnapTypes, promptMessage);

                points.Add(pickedPoint);
            }
            return points;
        }

    }
}
