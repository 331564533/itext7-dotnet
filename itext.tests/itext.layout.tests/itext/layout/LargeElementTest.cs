using System;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Layout.Element;
using iText.Test;

namespace iText.Layout {
    public class LargeElementTest : ExtendedITextTest {
        public static readonly String sourceFolder = NUnit.Framework.TestContext.CurrentContext.TestDirectory + "/../../resources/itext/layout/LargeElementTest/";

        public static readonly String destinationFolder = NUnit.Framework.TestContext.CurrentContext.TestDirectory
             + "/test/itext/layout/LargeElementTest/";

        [NUnit.Framework.OneTimeSetUp]
        public static void BeforeClass() {
            CreateDestinationFolder(destinationFolder);
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void LargeTableTest01() {
            String testName = "largeTableTest01.pdf";
            String outFileName = destinationFolder + testName;
            String cmpFileName = sourceFolder + "cmp_" + testName;
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(outFileName));
            Document doc = new Document(pdfDoc);
            Table table = new Table(5, true);
            doc.Add(table);
            for (int i = 0; i < 20; i++) {
                for (int j = 0; j < 5; j++) {
                    table.AddCell(new Cell().Add(new Paragraph(String.Format("Cell {0}, {1}", i + 1, j + 1))));
                }
                if (i % 10 == 0) {
                    table.Flush();
                    // This is a deliberate additional flush.
                    table.Flush();
                }
            }
            table.Complete();
            doc.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(outFileName, cmpFileName, destinationFolder
                , testName + "_diff"));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void LargeTableTest02() {
            String testName = "largeTableTest02.pdf";
            String outFileName = destinationFolder + testName;
            String cmpFileName = sourceFolder + "cmp_" + testName;
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(outFileName));
            Document doc = new Document(pdfDoc);
            Table table = new Table(5, true).SetMargins(20, 20, 20, 20);
            doc.Add(table);
            for (int i = 0; i < 100; i++) {
                table.AddCell(new Cell().Add(new Paragraph(String.Format("Cell {0}", i + 1))));
                if (i % 7 == 0) {
                    table.Flush();
                }
            }
            table.Complete();
            doc.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(outFileName, cmpFileName, destinationFolder
                , testName + "_diff"));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void LargeTableWithHeaderFooterTest01A() {
            String testName = "largeTableWithHeaderFooterTest01A.pdf";
            String outFileName = destinationFolder + testName;
            String cmpFileName = sourceFolder + "cmp_" + testName;
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(outFileName));
            Document doc = new Document(pdfDoc, PageSize.A4.Rotate());
            Table table = new Table(5, true);
            doc.Add(table);
            Cell cell = new Cell(1, 5).Add(new Paragraph("Table XYZ (Continued)"));
            table.AddHeaderCell(cell);
            cell = new Cell(1, 5).Add(new Paragraph("Continue on next page"));
            table.AddFooterCell(cell);
            table.SetSkipFirstHeader(true);
            table.SetSkipLastFooter(true);
            for (int i = 0; i < 350; i++) {
                table.AddCell(new Cell().Add(new Paragraph((i + 1).ToString())));
                table.Flush();
            }
            table.Complete();
            doc.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(outFileName, cmpFileName, destinationFolder
                , testName + "_diff"));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void LargeTableWithHeaderFooterTest01B() {
            String testName = "largeTableWithHeaderFooterTest01B.pdf";
            String outFileName = destinationFolder + testName;
            String cmpFileName = sourceFolder + "cmp_" + testName;
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(outFileName));
            Document doc = new Document(pdfDoc, PageSize.A4.Rotate());
            Table table = new Table(5, true);
            doc.Add(table);
            Cell cell = new Cell(1, 5).Add(new Paragraph("Table XYZ (Continued)"));
            table.AddHeaderCell(cell);
            cell = new Cell(1, 5).Add(new Paragraph("Continue on next page"));
            table.AddFooterCell(cell);
            table.SetSkipFirstHeader(true);
            table.SetSkipLastFooter(true);
            for (int i = 0; i < 350; i++) {
                table.Flush();
                table.AddCell(new Cell().Add(new Paragraph((i + 1).ToString())));
            }
            // That's the trick. complete() is called when table has non-empty content, so the last row is better laid out.
            // Compare with #largeTableWithHeaderFooterTest01A. When we flush last row before calling complete(), we don't yet know
            // if there will be any more rows. Flushing last row implicitly by calling complete solves this problem.
            table.Complete();
            doc.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(outFileName, cmpFileName, destinationFolder
                , testName + "_diff"));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void LargeTableWithHeaderFooterTest02() {
            String testName = "largeTableWithHeaderFooterTest02.pdf";
            String outFileName = destinationFolder + testName;
            String cmpFileName = sourceFolder + "cmp_" + testName;
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(outFileName));
            Document doc = new Document(pdfDoc, PageSize.A4.Rotate());
            Table table = new Table(5, true);
            doc.Add(table);
            for (int i = 0; i < 5; i++) {
                table.AddHeaderCell(new Cell().Add(new Paragraph("Header1 \n" + i)));
            }
            for (int i_1 = 0; i_1 < 5; i_1++) {
                table.AddHeaderCell(new Cell().Add(new Paragraph("Header2 \n" + i_1)));
            }
            for (int i_2 = 0; i_2 < 500; i_2++) {
                if (i_2 % 5 == 0) {
                    table.Flush();
                }
                table.AddCell(new Cell().Add(new Paragraph("Test " + i_2)));
            }
            table.Complete();
            doc.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(outFileName, cmpFileName, destinationFolder
                , testName + "_diff"));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void LargeTableWithHeaderFooterTest03() {
            String testName = "largeTableWithHeaderFooterTest03.pdf";
            String outFileName = destinationFolder + testName;
            String cmpFileName = sourceFolder + "cmp_" + testName;
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(outFileName));
            Document doc = new Document(pdfDoc, PageSize.A4.Rotate());
            Table table = new Table(5, true);
            doc.Add(table);
            for (int i = 0; i < 5; i++) {
                table.AddHeaderCell(new Cell().Add(new Paragraph("Header \n" + i)));
            }
            for (int i_1 = 0; i_1 < 5; i_1++) {
                table.AddFooterCell(new Cell().Add(new Paragraph("Footer \n" + i_1)));
            }
            for (int i_2 = 0; i_2 < 500; i_2++) {
                if (i_2 % 5 == 0) {
                    table.Flush();
                }
                table.AddCell(new Cell().Add(new Paragraph("Test " + i_2)));
            }
            table.Complete();
            doc.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(outFileName, cmpFileName, destinationFolder
                , testName + "_diff"));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void LargeTableWithHeaderFooterTest04() {
            String testName = "largeTableWithHeaderFooterTest04.pdf";
            String outFileName = destinationFolder + testName;
            String cmpFileName = sourceFolder + "cmp_" + testName;
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(outFileName));
            Document doc = new Document(pdfDoc, PageSize.A4.Rotate());
            Table table = new Table(5, true);
            doc.Add(table);
            for (int i = 0; i < 5; i++) {
                table.AddFooterCell(new Cell().Add(new Paragraph("Footer \n" + i)));
            }
            for (int i_1 = 0; i_1 < 500; i_1++) {
                if (i_1 % 5 == 0) {
                    table.Flush();
                }
                table.AddCell(new Cell().Add(new Paragraph("Test " + i_1)));
            }
            table.Complete();
            doc.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(outFileName, cmpFileName, destinationFolder
                , testName + "_diff"));
        }
    }
}
