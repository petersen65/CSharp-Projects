-- makecert -pe -r -n "CN=KISS ZoraClient" -sv "ZoraClientServiceCertificate.pvk" "ZoraClientServiceCertificate.cer"
CREATE CERTIFICATE [ZoraClientServiceCertificate] 
	AUTHORIZATION [ZoraClientServiceUser]
	FROM FILE = '$(CertificatesRootPath)\ZoraClientServiceCertificate.cer' 
	WITH PRIVATE KEY (FILE = '$(CertificatesRootPath)\ZoraClientServiceCertificate.pvk', DECRYPTION BY PASSWORD = 'pass@word1')
