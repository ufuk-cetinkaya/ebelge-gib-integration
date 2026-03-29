# e-Belge GİB Entegrasyonu

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


🚀 Dağıtım (Deployment)
Uygulamanın farklı ortamlardaki kurulum süreçleri için aşağıdaki ilgili altyapı depolarını inceleyebilirsiniz:

Cloud (AKS): Kubernetes manifestleri ile bulut ortamına dağıtım detayları için infra reposuna göz atın.

On-Prem / Local: Yerel makinelerde veya private cloud ortamlarında manuel kurulum (Docker-Desktop/K8s) için edonusum-gitops reposunu inceleyin.

🛠 Tech Stack & Architecture
Framework: .NET 10
Language: C# 14
IDE: Visual Studio 2026
Architecture: Microservices & Clean Architecture
API Style: Minimal APIs
Scheduler: Quartz.NET
ORM: EF CORE
Database Engine: MSSQL Server
Infrastructure: Docker & Kubernetes uyumlu (Cloud-Native)

🧪 Test ve Entegrasyon
Uygulamanın sunduğu uç noktaları (endpoints) test etmek için Tests klasöründe bir Postman Collection ile SoapUI projesi bulunmaktadır.
