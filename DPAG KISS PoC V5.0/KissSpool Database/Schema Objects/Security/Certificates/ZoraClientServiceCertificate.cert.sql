-- makecert -pe -r -n "CN=KISS ZoraClient" -sv "ZoraClientServiceCertificate.pvk" "ZoraClientServiceCertificate.cer"
CREATE CERTIFICATE [ZoraClientServiceCertificate] 
	AUTHORIZATION [ZoraClientServiceUser]
	FROM FILE = '$(CertificatesRootPath)\ZoraClientServiceCertificate.cer'
