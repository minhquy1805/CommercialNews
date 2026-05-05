# Deploy CommercialNews lên GCP VM bằng Docker Compose + Nginx + HTTPS

## 1. Mục tiêu

Tài liệu này ghi lại quy trình deploy dự án **CommercialNews** lên **Google Cloud VM** bằng:

- Docker Compose
- SQL Server container
- RabbitMQ container
- CommercialNews.Api container
- CommercialNews.Worker container
- Nginx reverse proxy
- Cloudflare DNS
- Let's Encrypt SSL certificate thông qua Certbot

Mô hình cuối cùng:

```txt
Client / Postman / Browser
        ↓
https://api.minhquy.dev
        ↓
Cloudflare DNS
        ↓
GCP VM External IP
        ↓
GCP Firewall 80/443
        ↓
Nginx container
        ↓
CommercialNews.Api container :8080
        ↓
SQL Server / RabbitMQ / Worker
```

---

## 2. Domain và DNS

Domain dùng cho API:

```txt
api.minhquy.dev
```

Trên Cloudflare tạo DNS record:

```txt
Type: A
Name: api
IPv4 address: <GCP_VM_STATIC_EXTERNAL_IP>
Proxy status: DNS only
TTL: Auto
```

Ví dụ:

```txt
A    api    35.198.220.175
```

Kiểm tra DNS:

```bash
nslookup api.minhquy.dev
```

Kết quả mong muốn:

```txt
Name: api.minhquy.dev
Address: <GCP_VM_STATIC_EXTERNAL_IP>
```

> Ghi chú: Nên dùng **Static External IP** cho VM. Nếu dùng Ephemeral IP, domain có thể trỏ sai sau khi stop/start VM.

---

## 3. GCP Firewall

Mở public các port web:

```txt
80   HTTP
443  HTTPS
```

Firewall rule HTTP:

```txt
Name: allow-commercialnews-http
Direction: Ingress
Action: Allow
Targets: Specified target tags
Target tags: commercial-news-vm
Source IPv4 ranges: 0.0.0.0/0
Protocols and ports: tcp:80
```

Firewall rule HTTPS:

```txt
Name: allow-commercialnews-https
Direction: Ingress
Action: Allow
Targets: Specified target tags
Target tags: commercial-news-vm
Source IPv4 ranges: 0.0.0.0/0
Protocols and ports: tcp:443
```

VM cần có network tag:

```txt
commercial-news-vm
```

Không mở public các port sau:

```txt
8080   API trực tiếp
1433   SQL Server
5672   RabbitMQ
15672  RabbitMQ Management
```

Production flow đúng:

```txt
Internet
    ↓
Nginx 80/443
    ↓
CommercialNews.Api 8080 internal
```

---

## 4. Cấu trúc Docker cần có

```txt
docker/
├── api/
│   └── Dockerfile
├── worker/
│   └── Dockerfile
├── nginx/
│   ├── nginx.conf
│   └── nginx.prod.conf
├── certbot/
│   ├── www/
│   └── conf/
├── docker-compose.dev.yaml
├── docker-compose.prod.yaml
├── .env.dev
├── .env.prod
└── .env.example
```

Không commit các file/thư mục chứa secret:

```txt
docker/.env.dev
docker/.env.prod
docker/certbot/conf/
docker/certbot/www/
```

`.gitignore` nên có:

```gitignore
# Environment files
.env
.env.*
**/.env
**/.env.*

# Keep templates
!.env.example
!**/.env.example
!docker/.env.prod.example

# Certbot / Let's Encrypt
docker/certbot/conf/
docker/certbot/www/
```

---

## 5. Production Docker Compose

File:

```txt
docker/docker-compose.prod.yaml
```

Điểm quan trọng:

API không public port `8080`:

```yaml
commercialnews-api:
  expose:
    - "8080"
```

Nginx public port `80` và `443`:

```yaml
nginx:
  image: nginx:1.27-alpine
  container_name: commercialnews-nginx
  depends_on:
    - commercialnews-api
  ports:
    - "80:80"
    - "443:443"
  volumes:
    - ./nginx/nginx.prod.conf:/etc/nginx/nginx.conf:ro
    - ./certbot/www:/var/www/certbot:ro
    - ./certbot/conf:/etc/letsencrypt:ro
  restart: unless-stopped
```

Certbot service:

```yaml
certbot:
  image: certbot/certbot:latest
  container_name: commercialnews-certbot
  volumes:
    - ./certbot/www:/var/www/certbot
    - ./certbot/conf:/etc/letsencrypt
```

---

## 6. Production environment file

File thật chỉ đặt trên VM:

```txt
docker/.env.prod
```

Các biến quan trọng:

```env
API_ALLOWED_HOSTS=api.minhquy.dev
API_JWT_ISSUER=https://api.minhquy.dev
WORKER_PUBLIC_FRONTEND_BASE_URL=https://api.minhquy.dev
```

API connection string dùng service name `cn-mssql`:

```env
API_SQL_CONNECTION_STRING=Server=cn-mssql,1433;Database=CommercialNews;User Id=cn_api_login;Password=<API_DB_PASSWORD>;TrustServerCertificate=True;Encrypt=True;
```

Worker connection string:

```env
WORKER_SQL_CONNECTION_STRING=Server=cn-mssql,1433;Database=CommercialNews;User Id=cn_worker_login;Password=<WORKER_DB_PASSWORD>;TrustServerCertificate=True;Encrypt=True;
```

RabbitMQ host dùng service name:

```env
RABBITMQ_HOST=rabbitmq
RABBITMQ_PORT=5672
```

Set quyền cho `.env.prod`:

```bash
chmod 600 docker/.env.prod
```

---

## 7. Clone/Pull repo trên VM

SSH vào VM:

```bash
ssh <vm-user>@<VM_EXTERNAL_IP>
```

Nếu chưa clone:

```bash
mkdir -p ~/projects
cd ~/projects
git clone git@github.com:<your-user>/<your-repo>.git CommercialNews
cd CommercialNews
```

Nếu repo đã có:

```bash
cd ~/projects/CommercialNews
git pull origin main
```

---

## 8. Tạo thư mục Certbot trên VM

```bash
cd ~/projects/CommercialNews

mkdir -p docker/certbot/www
mkdir -p docker/certbot/conf
```

---

## 9. Chạy Docker Compose production

```bash
docker compose \
  -f docker/docker-compose.prod.yaml \
  --env-file docker/.env.prod \
  up -d --build
```

Kiểm tra container:

```bash
docker ps
```

Các container mong muốn:

```txt
commercialnews-nginx
commercialnews-api
commercialnews-worker
cn-mssql
cn-rabbitmq
```

---

## 10. Bootstrap database

SQL Server container chỉ chạy engine. Cần chạy scripts để tạo:

- Database
- Schemas
- Logins/users
- Roles/grants
- Tables
- Indexes
- Stored procedures

Copy folder `db` vào SQL Server container:

```bash
docker cp db cn-mssql:/var/opt/mssql/db
```

Chạy tạo database:

```bash
docker exec -it cn-mssql /opt/mssql-tools18/bin/sqlcmd \
-S localhost \
-U sa \
-P '<SA_PASSWORD>' \
-C \
-b \
-i /var/opt/mssql/db/00_bootstrap/001_create_database.sql
```

Chạy tạo schemas:

```bash
docker exec -it cn-mssql /opt/mssql-tools18/bin/sqlcmd \
-S localhost \
-U sa \
-P '<SA_PASSWORD>' \
-C \
-b \
-d CommercialNews \
-i /var/opt/mssql/db/00_bootstrap/010_create_schemas.sql
```

Chạy tạo logins/users:

```bash
docker exec -it cn-mssql /opt/mssql-tools18/bin/sqlcmd \
-S localhost \
-U sa \
-P '<SA_PASSWORD>' \
-C \
-b \
-d CommercialNews \
-i /var/opt/mssql/db/00_bootstrap/020_create_logins_users.local.sql
```

Chạy roles:

```bash
docker exec -it cn-mssql /opt/mssql-tools18/bin/sqlcmd \
-S localhost \
-U sa \
-P '<SA_PASSWORD>' \
-C \
-b \
-d CommercialNews \
-i /var/opt/mssql/db/00_bootstrap/030_create_roles.sql
```

Chạy grants:

```bash
docker exec -it cn-mssql /opt/mssql-tools18/bin/sqlcmd \
-S localhost \
-U sa \
-P '<SA_PASSWORD>' \
-C \
-b \
-d CommercialNews \
-i /var/opt/mssql/db/00_bootstrap/040_grants_baseline.sql
```

Chạy module scripts:

```bash
for module in outbox identity authorization notifications audit content media seo interaction reading; do
  for file in 001_tables.sql 010_indexes.sql 020_procs.sql; do
    if [ -f "db/10_modules/$module/$file" ]; then
      echo "Running $module/$file"
      docker exec -i cn-mssql /opt/mssql-tools18/bin/sqlcmd \
        -S localhost \
        -U sa \
        -P '<SA_PASSWORD>' \
        -C \
        -b \
        -d CommercialNews \
        -i "/var/opt/mssql/db/10_modules/$module/$file"
    fi
  done
done
```

Restart API/Worker/Nginx:

```bash
docker restart commercialnews-api commercialnews-worker commercialnews-nginx
```

---

## 11. Test HTTP trước HTTPS

Test nội bộ trên VM với Host header:

```bash
curl -i -H "Host: api.minhquy.dev" http://localhost/health/live
curl -i -H "Host: api.minhquy.dev" http://localhost/health/ready
```

Test qua domain:

```bash
curl -i http://api.minhquy.dev/health/live
curl -i http://api.minhquy.dev/health/ready
```

Kết quả mong muốn:

```txt
HTTP/1.1 200 OK
Healthy
```

Nếu gọi `http://localhost/...` mà bị:

```txt
400 Bad Request - Invalid Hostname
```

nguyên nhân là:

```env
API_ALLOWED_HOSTS=api.minhquy.dev
```

Cách test đúng:

```bash
curl -i -H "Host: api.minhquy.dev" http://localhost/health/live
```

---

## 12. Nginx HTTP challenge config trước khi xin SSL

Trước khi có certificate, `nginx.prod.conf` không được dùng `listen 443 ssl`.

File:

```txt
docker/nginx/nginx.prod.conf
```

Bản HTTP challenge:

```nginx
events {}

http {
    server {
        listen 80;
        server_name api.minhquy.dev;

        client_max_body_size 20M;

        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
        }

        location / {
            proxy_pass http://commercialnews-api:8080;

            proxy_http_version 1.1;

            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;

            proxy_connect_timeout 30s;
            proxy_send_timeout 30s;
            proxy_read_timeout 30s;
        }
    }
}
```

Apply compose:

```bash
docker compose \
  -f docker/docker-compose.prod.yaml \
  --env-file docker/.env.prod \
  up -d
```

Test HTTP:

```bash
curl -i http://api.minhquy.dev/health/live
```

---

## 13. Xin SSL certificate bằng Certbot

```bash
docker compose \
  -f docker/docker-compose.prod.yaml \
  --env-file docker/.env.prod \
  run --rm certbot certonly \
  --webroot \
  --webroot-path /var/www/certbot \
  -d api.minhquy.dev \
  --email <your-email@example.com> \
  --agree-tos \
  --no-eff-email
```

Kết quả thành công:

```txt
Successfully received certificate.
Certificate is saved at: /etc/letsencrypt/live/api.minhquy.dev/fullchain.pem
Key is saved at:         /etc/letsencrypt/live/api.minhquy.dev/privkey.pem
```

Trên VM, cert nằm trong:

```txt
docker/certbot/conf/live/api.minhquy.dev/
```

Nếu user thường không xem được:

```bash
sudo ls -la docker/certbot/conf/live/api.minhquy.dev
```

---

## 14. Nginx HTTPS config

Sau khi cert tồn tại, đổi `docker/nginx/nginx.prod.conf` thành:

```nginx
events {}

http {
    server {
        listen 80;
        server_name api.minhquy.dev;

        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
        }

        location / {
            return 301 https://$host$request_uri;
        }
    }

    server {
        listen 443 ssl;
        server_name api.minhquy.dev;

        ssl_certificate /etc/letsencrypt/live/api.minhquy.dev/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/api.minhquy.dev/privkey.pem;

        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_prefer_server_ciphers off;

        client_max_body_size 20M;

        location / {
            proxy_pass http://commercialnews-api:8080;

            proxy_http_version 1.1;

            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto https;

            proxy_connect_timeout 30s;
            proxy_send_timeout 30s;
            proxy_read_timeout 30s;
        }
    }
}
```

Test Nginx config:

```bash
docker exec -it commercialnews-nginx nginx -t
```

Kết quả mong muốn:

```txt
syntax is ok
test is successful
```

Restart Nginx:

```bash
docker restart commercialnews-nginx
```

---

## 15. Test HTTPS

Test HTTPS health:

```bash
curl -i https://api.minhquy.dev/health/live
curl -i https://api.minhquy.dev/health/ready
```

Kết quả mong muốn:

```txt
HTTP/1.1 200 OK
Healthy
```

Test HTTP redirect:

```bash
curl -I http://api.minhquy.dev/health/live
```

Kết quả mong muốn:

```txt
HTTP/1.1 301 Moved Permanently
Location: https://api.minhquy.dev/health/live
```

---

## 16. Test API thật

Login endpoint:

```txt
POST https://api.minhquy.dev/api/v1/identity/login
```

Body ví dụ:

```json
{
  "email": "<admin-email>",
  "password": "<admin-password>"
}
```

Kết quả mong muốn:

```txt
200 OK
accessToken
refreshToken
userId
email
expiresAt
```

---

## 17. Sửa `Program.cs` cho mô hình Nginx

Vì production HTTPS do Nginx xử lý, API không cần tự redirect HTTPS.

Trong `Program.cs`, không nên để production gọi trực tiếp:

```csharp
app.UseHttpsRedirection();
```

Nên dùng:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

Production flow:

```txt
Client HTTPS
    ↓
Nginx 443
    ↓ HTTP internal
CommercialNews.Api 8080
```

Nginx chịu trách nhiệm:

```txt
HTTP 80 → HTTPS 443
```

---

## 18. Swagger trong Production

Swagger chỉ bật trong Development:

```csharp
if (!app.Environment.IsDevelopment())
{
    return app;
}
```

Production không public Swagger.

Test production bằng:

```txt
/health/live
/health/ready
```

và API thật.

---

## 19. Logs và debug

Xem container:

```bash
docker ps
```

Xem logs:

```bash
docker logs commercialnews-api --tail 100
docker logs commercialnews-worker --tail 100
docker logs commercialnews-nginx --tail 100
```

Test Nginx gọi API nội bộ:

```bash
docker exec -it commercialnews-nginx sh -c "wget -S -O- http://commercialnews-api:8080/health/live"
```

Restart Nginx:

```bash
docker restart commercialnews-nginx
```

Restart API/Worker:

```bash
docker restart commercialnews-api commercialnews-worker
```

---

## 20. Các lỗi đã gặp và cách xử lý

### 20.1. `502 Bad Gateway`

Ý nghĩa:

```txt
Nginx nhận request nhưng không gọi được API upstream.
```

Kiểm tra:

```bash
docker logs commercialnews-api --tail 100
docker logs commercialnews-nginx --tail 100
docker exec -it commercialnews-nginx sh -c "wget -S -O- http://commercialnews-api:8080/health/live"
```

Nguyên nhân từng gặp:

```txt
API chưa start
DB script chưa chạy
Nginx cần restart sau khi API recreate
```

---

### 20.2. `Login failed for user 'cn_api_login'`

Nguyên nhân:

```txt
Chưa tạo SQL login/user hoặc password không khớp .env.prod.
```

Cách xử lý:

```txt
Chạy 020_create_logins_users.local.sql
Đảm bảo password khớp API_SQL_CONNECTION_STRING
```

---

### 20.3. `Could not find stored procedure`

Ví dụ:

```txt
Could not find stored procedure 'identity.UserAccount_SelectByEmailNormalized'
```

Nguyên nhân:

```txt
Chưa chạy module procs script.
```

Cách xử lý:

```bash
docker exec -it cn-mssql /opt/mssql-tools18/bin/sqlcmd \
-S localhost \
-U sa \
-P '<SA_PASSWORD>' \
-C \
-b \
-d CommercialNews \
-i /var/opt/mssql/db/10_modules/identity/020_procs.sql
```

Hoặc chạy lại toàn bộ module scripts.

---

### 20.4. `400 Bad Request - Invalid Hostname`

Nguyên nhân:

```txt
API_ALLOWED_HOSTS=api.minhquy.dev
```

nhưng request dùng:

```txt
Host: localhost
```

Cách test đúng:

```bash
curl -i -H "Host: api.minhquy.dev" http://localhost/health/live
```

---

### 20.5. `Permission denied` khi xem certbot folder

Ví dụ:

```txt
ls: cannot access 'docker/certbot/conf/live/api.minhquy.dev': Permission denied
```

Nguyên nhân:

```txt
Certificate/private key thuộc root.
```

Cách kiểm tra:

```bash
sudo ls -la docker/certbot/conf/live/api.minhquy.dev
```

---

## 21. Cert renew

Test renew:

```bash
docker compose \
  -f docker/docker-compose.prod.yaml \
  --env-file docker/.env.prod \
  run --rm certbot renew --dry-run
```

Sau này cần setup cron job để renew tự động.

Ví dụ ý tưởng:

```bash
docker compose \
  -f /home/<user>/projects/CommercialNews/docker/docker-compose.prod.yaml \
  --env-file /home/<user>/projects/CommercialNews/docker/.env.prod \
  run --rm certbot renew

docker restart commercialnews-nginx
```

---

## 22. Kết quả cuối cùng

Production URL:

```txt
https://api.minhquy.dev
```

Health endpoints:

```txt
https://api.minhquy.dev/health/live
https://api.minhquy.dev/health/ready
```

Login endpoint:

```txt
https://api.minhquy.dev/api/v1/identity/login
```

Final architecture:

```txt
Cloudflare DNS
    ↓
GCP VM
    ↓
Nginx :443 SSL
    ↓
CommercialNews.Api :8080
    ↓
SQL Server / RabbitMQ / Worker
```

Deployment hiện tại đã đạt:

```txt
Docker Compose production
Nginx reverse proxy
HTTPS with Let's Encrypt
Domain through Cloudflare
Health check OK
Login API OK
Worker OK
SQL Server OK
RabbitMQ OK
```