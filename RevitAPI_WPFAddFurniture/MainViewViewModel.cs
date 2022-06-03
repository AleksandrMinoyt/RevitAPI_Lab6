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

namespace RevitAPI_WPFAddFurniture
{
    class MainViewViewModel
    {
        private ExternalCommandData _commandData;

        //Список всех типов мебели
        public List<FamilySymbol> FurnitureTypes { get; private set; } = new List<FamilySymbol>();
        public FamilySymbol SelectedFurnitureType { get; set; }

        //Список уровней
        public List<Level> Levels { get; private set; } = new List<Level>();
        public Level SelectedLevel { get; set; }

        public DelegateCommand AddFurniture { get; private set; }

        public XYZ InsertPoint { get; private set; } = new XYZ();


        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;

            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //Находим все типы мебели
            FurnitureTypes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Furniture)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()                
                .ToList();

            //Находим все уровни
            Levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();


            AddFurniture = new DelegateCommand(OnAddFurniture);

            //Выбор точек
            InsertPoint = GetPoints(_commandData, "Выберите точки", ObjectSnapTypes.Endpoints, 1)[0];
        }

        private void OnAddFurniture()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (SelectedFurnitureType == null ||
                SelectedLevel == null)
                return;

            using (var trans = new Transaction(doc, "добавляем мебель"))
            {
                trans.Start();

                if (!SelectedFurnitureType.IsActive)
                    SelectedFurnitureType.Activate();
                //NewFamilyInstance(XYZ location, DB.FamilySymbol symbol, Level level, StructuralType structuralType);
                doc.Create.NewFamilyInstance(InsertPoint, SelectedFurnitureType, SelectedLevel, StructuralType.NonStructural);


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
