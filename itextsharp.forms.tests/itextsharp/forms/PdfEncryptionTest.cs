using System;
using System.IO;
using NUnit.Framework;
using iTextSharp.Kernel.Pdf;

namespace iTextSharp.Forms
{
	public class PdfEncryptionTest
	{
		public const String sourceFolder = "../../resources/itextsharp/forms/PdfEncryptionTest/";

		/// <exception cref="System.IO.IOException"/>
		[Test]
		[Ignore]
		public virtual void EncryptedDocumentWithFormFields()
		{
			PdfReader reader = new PdfReader(new FileStream(sourceFolder + "encryptedDocumentWithFormFields.pdf"
				), new ReaderProperties().SetPassword("12345".GetBytes()));
			PdfDocument pdfDocument = new PdfDocument(reader);
			PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(pdfDocument, false);
			acroForm.GetField("name").GetPdfObject();
			pdfDocument.Close();
		}
	}
}