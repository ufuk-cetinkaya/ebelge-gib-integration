# EBelgeGibIntegration

Bu repoda e-fatura ve e-irsaliye gönderme, alma işlemleriyle e-arşiv fatura raporlama süreçlerinin GİB (Gelir İdaresi Başkanlığı) standartlarına uygun olarak yöneten uygulamalar bulunmaktadır.

🚀 Bileşenler ve Görev Dağılımı.

1. document-api: Belge yönetimini kontrol eden uç noktaları sağlar.
- POST /document: UBL belgeleri kontrol ederek sisteme alır.
- GET /document?query: Belirtilen kriterlere uygun belgelerin listesini döner.
- GET /preview/uuid/type: Belgeleri xml, html ve pdf formatlarına dönüştürür.
- DELETE /cancel: e-Arşiv belgeleri için iptal isteği gönderilebilmesini sağlar.

2. envelope-api: SoapCore kullanılarak geliştirilmiş (SOAP-over-REST) uygulamasıdır.
- sendDocument: GİB'in gönderdiği zarfları kontrol ederek içeri alır.
- getApplicationResponse: Zarfların durum bilgisini içeren uygulama yanıtları döner.

3. envelope-worker: Quartz.NET tarafından yönetilen 5 adet job ile e-fatura ve e-irsaliye trafiğini yönetir.
- SignDocuments: İmzalanacak belgeleri tespit eder ve signer-ws üzerinden imzalama sürecini tamamlar.
- CreateEnvelopes: İmzalı belgeleri zarflar. Gönderici/Alıcı bilgilerini gib-user-api den alır.
- SendEnvelopes: Hazırlanan zarfları GİB'in servisine gönderir.
- CheckStatus: Gönderilen zarfların durumunu GİB servisinden sorgular.
- ReceiveEnvelopes: GİB'den gelen zarfların içeriğindeki belgeleri işler.

4. report-worker: Quartz.NET tarafından yönetilen 5 adet job ile e-arşiv raporlama sürecini yönetir.
- LoadReports: e-Arşiv raporu oluşturur.
- PackageReports: e-Arşiv belgelerini rapora dahil eder.
- SignReports: Raporları signer-ws servisine imzalatır.
- SendReports: Raporları GİB'in servisine gönderir.
- CheckStatus: Rapor durumlarını GİB servisinden sorgular.

🛠 Teknik Yığın
Framework: .NET 10
Dil: C# 14
Zamanlayıcı: Quartz.NET
IDE: Visual Studio 2026
ORM: EF CORE
