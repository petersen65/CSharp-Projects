-- makecert -pe -r -n "CN=KISS Processing" -sv "KissProcessingServiceCertificate.pvk" "KissProcessingServiceCertificate.cer"
CREATE CERTIFICATE [KissProcessingServiceCertificate] 
	AUTHORIZATION [KissProcessingServiceUser]
	FROM FILE = '$(CertificatesRootPath)\KissProcessingServiceCertificate.cer' 
	WITH PRIVATE KEY (FILE = '$(CertificatesRootPath)\KissProcessingServiceCertificate.pvk', DECRYPTION BY PASSWORD = 'pass@word1')
