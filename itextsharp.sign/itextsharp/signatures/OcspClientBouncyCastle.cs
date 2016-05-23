/*
$Id$

This file is part of the iText (R) project.
Copyright (c) 1998-2016 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.IO;
using Java.IO;
using Java.Math;
using Java.Net;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using Org.Bouncycastle.Asn1.Ocsp;
using Org.Bouncycastle.Asn1.X509;
using Org.Bouncycastle.Cert.Jcajce;
using Org.Bouncycastle.Cert.Ocsp;
using Org.Bouncycastle.Jce.Provider;
using Org.Bouncycastle.Ocsp;
using Org.Bouncycastle.Operator.Jcajce;
using iTextSharp.IO;
using iTextSharp.IO.Log;
using iTextSharp.IO.Util;
using iTextSharp.Kernel;
using iTextSharp.Kernel.Pdf;

namespace iTextSharp.Signatures
{
	/// <summary>OcspClient implementation using BouncyCastle.</summary>
	/// <author>Paulo Soarees</author>
	public class OcspClientBouncyCastle : IOcspClient
	{
		/// <summary>The Logger instance.</summary>
		private static readonly ILogger LOGGER = LoggerFactory.GetLogger(typeof(iTextSharp.Signatures.OcspClientBouncyCastle
			));

		private readonly OCSPVerifier verifier;

		/// <summary>
		/// Create
		/// <c>OcspClient</c>
		/// </summary>
		/// <param name="verifier">
		/// will be used for response verification.
		/// <seealso>OCSPVerifier</seealso>
		/// .
		/// </param>
		public OcspClientBouncyCastle(OCSPVerifier verifier)
		{
			this.verifier = verifier;
		}

		/// <summary>Gets OCSP response.</summary>
		/// <remarks>
		/// Gets OCSP response. If
		/// <seealso>OCSPVerifier</seealso>
		/// was setted, the response will be checked.
		/// </remarks>
		public virtual BasicOCSPResp GetBasicOCSPResp(X509Certificate checkCert, X509Certificate
			 rootCert, String url)
		{
			try
			{
				OCSPResp ocspResponse = GetOcspResponse(checkCert, rootCert, url);
				if (ocspResponse == null)
				{
					return null;
				}
				if (ocspResponse.GetStatus() != OCSPRespStatus.SUCCESSFUL)
				{
					return null;
				}
				BasicOCSPResp basicResponse = (BasicOCSPResp)ocspResponse.GetResponseObject();
				if (verifier != null)
				{
					verifier.IsValidResponse(basicResponse, rootCert);
				}
				return basicResponse;
			}
			catch (Exception ex)
			{
				LOGGER.Error(ex.Message);
			}
			return null;
		}

		/// <summary>Gets an encoded byte array with OCSP validation.</summary>
		/// <remarks>Gets an encoded byte array with OCSP validation. The method should not throw an exception.
		/// 	</remarks>
		/// <param name="checkCert">to certificate to check</param>
		/// <param name="rootCert">the parent certificate</param>
		/// <param name="url">
		/// to get the verification. It it's null it will be taken
		/// from the check cert or from other implementation specific source
		/// </param>
		/// <returns>a byte array with the validation or null if the validation could not be obtained
		/// 	</returns>
		public virtual byte[] GetEncoded(X509Certificate checkCert, X509Certificate rootCert
			, String url)
		{
			try
			{
				BasicOCSPResp basicResponse = GetBasicOCSPResp(checkCert, rootCert, url);
				if (basicResponse != null)
				{
					SingleResp[] responses = basicResponse.GetResponses();
					if (responses.Length == 1)
					{
						SingleResp resp = responses[0];
						Object status = resp.GetCertStatus();
						if (status == CertificateStatus.GOOD)
						{
							return basicResponse.GetEncoded();
						}
						else
						{
							if (status is RevokedStatus)
							{
								throw new System.IO.IOException(LogMessageConstant.OCSP_STATUS_IS_REVOKED);
							}
							else
							{
								throw new System.IO.IOException(LogMessageConstant.OCSP_STATUS_IS_UNKNOWN);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				LOGGER.Error(ex.Message);
			}
			return null;
		}

		/// <summary>Generates an OCSP request using BouncyCastle.</summary>
		/// <param name="issuerCert">certificate of the issues</param>
		/// <param name="serialNumber">serial number</param>
		/// <returns>an OCSP request</returns>
		/// <exception cref="Org.Bouncycastle.Cert.Ocsp.OCSPException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Bouncycastle.Operator.OperatorException"/>
		/// <exception cref="Java.Security.Cert.CertificateEncodingException"/>
		private static OCSPReq GenerateOCSPRequest(X509Certificate issuerCert, BigInteger
			 serialNumber)
		{
			//Add provider BC
			Java.Security.Security.AddProvider(new BouncyCastleProvider());
			// Generate the id for the certificate we are looking for
			CertificateID id = new CertificateID(new JcaDigestCalculatorProviderBuilder().Build
				().Get(CertificateID.HASH_SHA1), new JcaX509CertificateHolder(issuerCert), serialNumber
				);
			// basic request generation with nonce
			OCSPReqBuilder gen = new OCSPReqBuilder();
			gen.AddRequest(id);
			X509Extensions ext = new X509Extensions(OCSPObjectIdentifiers.id_pkix_ocsp_nonce, 
				false, new DerOctetString(new DerOctetString(PdfEncryption.GenerateNewDocumentId
				()).GetEncoded()));
			gen.SetRequestExtensions(new Extensions(new X509Extensions[] { ext }));
			return gen.Build();
		}

		/// <exception cref="Org.BouncyCastle.Security.GeneralSecurityException"/>
		/// <exception cref="Org.Bouncycastle.Cert.Ocsp.OCSPException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Org.Bouncycastle.Operator.OperatorException"/>
		private OCSPResp GetOcspResponse(X509Certificate checkCert, X509Certificate rootCert
			, String url)
		{
			if (checkCert == null || rootCert == null)
			{
				return null;
			}
			if (url == null)
			{
				url = CertificateUtil.GetOCSPURL(checkCert);
			}
			if (url == null)
			{
				return null;
			}
			LOGGER.Info("Getting OCSP from " + url);
			OCSPReq request = GenerateOCSPRequest(rootCert, checkCert.GetSerialNumber());
			byte[] array = request.GetEncoded();
			Uri urlt = new Uri(url);
			HttpURLConnection con = (HttpURLConnection)urlt.OpenConnection();
			con.SetRequestProperty("Content-Type", "application/ocsp-request");
			con.SetRequestProperty("Accept", "application/ocsp-response");
			con.SetDoOutput(true);
			Stream @out = con.GetOutputStream();
			DataOutputStream dataOut = new DataOutputStream(new BufferedOutputStream(@out));
			dataOut.Write(array);
			dataOut.Flush();
			dataOut.Close();
			if (con.GetResponseCode() / 100 != 2)
			{
				throw new PdfException(PdfException.InvalidHttpResponse1).SetMessageParams(con.GetResponseCode
					());
			}
			//Get Response
			Stream @in = (Stream)con.GetContent();
			return new OCSPResp(StreamUtil.InputStreamToArray(@in));
		}
	}
}