-- makecert -pe -r -n "CN=KISS Processing" -sv "KissProcessingServiceCertificate.pvk" "KissProcessingServiceCertificate.cer"
CREATE CERTIFICATE [KissProcessingServiceCertificate] 
	AUTHORIZATION [KissProcessingServiceUser]
	FROM FILE = '$(CertificatesRootPath)\KissProcessingServiceCertificate.cer' 
