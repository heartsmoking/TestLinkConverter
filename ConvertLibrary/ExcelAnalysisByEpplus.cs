﻿using System;
using System.IO;
using System.Drawing;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using log4net;
using ConvertLibrary;
using ConvertModel;

namespace ConvertLibrary
{
    public class ExcelAnalysisByEpplus
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(ExcelAnalysisByEpplus));
        private readonly ExcelPackage _excelPackage;


        public ExcelAnalysisByEpplus(string excelFilePath)
        {
            if (string.IsNullOrEmpty(excelFilePath))
            {
                OutputDisplay.ShowMessage("传入文件地址有误！", Color.Red);
                return;
            }

            if (!File.Exists(excelFilePath))
            {
                OutputDisplay.ShowMessage("文件不存在!", Color.Red);
                return;
            }

            try
            {
                FileInfo fiExcel = new System.IO.FileInfo(excelFilePath);
                this._excelPackage = new ExcelPackage(fiExcel);
            }catch (Exception ex)
            {
                OutputDisplay.ShowMessage(ex.Message, Color.Red);
                return;
            }
        }

        public Dictionary<string, List<TestCase>> ReadExcel()
        {
            Dictionary<string, List<TestCase>> dicAllTestCases = new Dictionary<string, List<TestCase>>();
            int iCount = this._excelPackage.Workbook.Worksheets.Count;

            for(int iFlag = 1; iFlag <= iCount; iFlag++)
            {
                ExcelWorksheet excelWorksheet = this._excelPackage.Workbook.Worksheets[iFlag];
                var testCase = this.GetExcelSheetData(excelWorksheet);

                if(testCase.Count == 0)
                {
                    OutputDisplay.ShowMessage($"页签:{excelWorksheet.Name}无任何可转换用例数据.", Color.GreenYellow);
                    continue;
                }

                dicAllTestCases.Add(excelWorksheet.Name, testCase);
            }
            this._excelPackage.Dispose();
            return dicAllTestCases;
        }

        public List<TestCase> GetExcelSheetData(ExcelWorksheet eWorksheet)
        {
            List<TestCase> tcList = new List<TestCase>();
            int usedRows, usedCols;

            if(eWorksheet.Dimension == null)
            {
                this._logger.Warn(new Exception("No TestCase, this Sheet is new!"));
                return new List<TestCase>();
            }
            else
            {
                usedRows = eWorksheet.Dimension.End.Row;
                usedCols = eWorksheet.Dimension.End.Column;
            }

            if(usedRows == 0 || usedRows == 1)
            {
                this._logger.Warn(new Exception("No TestCase!"));
                return tcList;
            }

            for(int i=1; i < eWorksheet.Dimension.End.Row; i++)
            {
                if(eWorksheet.Cells[i,1].Value != null || eWorksheet.Cells[i,1].Text != string.Empty ||
                    !eWorksheet.Cells[i, 1].Text.Equals("END"))
                {
                    continue;
                }
                usedRows = i;
                break;
            }

            TestCase tc = new TestCase();

            for (int i = 2; i <= usedRows; i++)
            {
                var currentCell = eWorksheet.Cells[i, 1];
                //设置单元格格式为文本格式，防止为自定义格式时读取单元格报错
                for (int j = 2; j <= usedCols; j++)
                {
                    eWorksheet.Cells[i, j].Style.Numberformat.Format = "@";
                }

                if (currentCell.Value == null)
                {
                    TestStep ts = new TestStep
                    {
                        StepNumber = tc.TestSteps.Count + 1,
                        ExecutionType = ExecType.手动,
                        Actions = eWorksheet.Cells[i, usedCols-1].Text,
                        ExpectedResults = eWorksheet.Cells[i, usedCols].Text
                    };

                    tc.TestSteps.Add(ts);
                    continue;
                }
                else
                {
                    if(tc.ExternalId != null)
                    {
                        tcList.Add(tc);
                    }

                    List<string> testSuitesName = new List<string>();
                    if (usedCols > 9)
                    {
                        for (int j = 1; j <= usedCols-9; j++)
                        {
                            if (eWorksheet.Cells[i, j + 1].Value != null)
                            {
                                testSuitesName.Add(eWorksheet.Cells[i, j+1].Text);
                            }
                        }
                    }

                    tc = new TestCase
                    {
                        ExternalId = string.Format($"{currentCell.Text}_{new Random().Next(0, 10000)}"),
                        TestCaseHierarchy = testSuitesName,
                        Name = eWorksheet.Cells[i, usedCols-7].Text,
                        Keywords = eWorksheet.Cells[i, usedCols-6].Text.Split(',').ToList(),
                        Importance = CommonHelper.StrToImportanceType(eWorksheet.Cells[i, usedCols-5].Text),
                        ExecutionType = CommonHelper.StrToExecType(eWorksheet.Cells[i, usedCols-4].Text),
                        Summary = eWorksheet.Cells[i, usedCols-3].Text,
                        Preconditions = eWorksheet.Cells[i, usedCols-2].Text
                    };
                    
                    TestStep tsOne = new TestStep
                    {
                        StepNumber = 1,
                        ExecutionType = ExecType.手动,
                        Actions = eWorksheet.Cells[i, usedCols-1].Text.ToString(),
                        ExpectedResults = eWorksheet.Cells[i, usedCols].Text.ToString()
                    };

                    tc.TestSteps = new List<TestStep> {tsOne};
                }
            }

            return tcList;
        }


    }
}
