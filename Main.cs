﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPITrainingPlotExport
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            var sheets = new FilteredElementCollector(doc)
                                .WhereElementIsNotElementType()
                                .OfClass(typeof(ViewSheet))
                                .Cast<ViewSheet>()
                                .ToList();

            var groupedSheets = sheets.GroupBy(sheet => doc.GetElement(new FilteredElementCollector(doc, sheet.Id)
                                        .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                        .FirstElementId()).Name);

            var viewSets = new List<ViewSet>();

            PrintManager printManager = doc.PrintManager;
            printManager.SelectNewPrintDriver("PDFCreator");
            printManager.PrintRange = PrintRange.Select;
            ViewSheetSetting viewSheetSetting = printManager.ViewSheetSetting;

            foreach (var groupedSheet in groupedSheets)
            {
                if (groupedSheet.Key == null)
                    continue;

                var viewSet = new ViewSet();

                var sheetsOfGroup = groupedSheet.Select(s => s).ToList();
                foreach (var sheet in sheetsOfGroup)
                {
                    viewSet.Insert(sheet);
                }

                viewSets.Add(viewSet);

                printManager.PrintRange = PrintRange.Select;
                viewSheetSetting.CurrentViewSheetSet.Views = viewSet;

                using (var ts = new Transaction(doc, "Create view set"))
                {
                    ts.Start();
                    viewSheetSetting.SaveAs($"{groupedSheet.Key}_{Guid.NewGuid()}");
                    ts.Commit();
                }

                bool isFormartSelected = false;
                foreach (PaperSize paperSize in printManager.PaperSizes)
                {
                    if (string.Equals(groupedSheet.Key, "А4К") &&
                        string.Equals(paperSize.Name, "A4"))
                    {
                        printManager.PrintSetup.CurrentPrintSetting.PrintParameters.PaperSize = paperSize;
                        printManager.PrintSetup.CurrentPrintSetting.PrintParameters.PageOrientation = PageOrientationType.Portrait;
                        isFormartSelected = true;
                    }
                    else if (string.Equals(groupedSheet.Key, "А3А") &&
                        string.Equals(paperSize.Name, "A3"))
                    {
                        printManager.PrintSetup.CurrentPrintSetting.PrintParameters.PaperSize = paperSize;
                        printManager.PrintSetup.CurrentPrintSetting.PrintParameters.PageOrientation = PageOrientationType.Landscape;
                        isFormartSelected = true;
                    }
                }

                if (!isFormartSelected)
                {
                    TaskDialog.Show("Ошибка", "Не найден формат");
                    return Result.Failed;
                }

                printManager.CombinedFile = false;
                printManager.SubmitPrint();
            }


            return Result.Succeeded;
        }
    }
}
