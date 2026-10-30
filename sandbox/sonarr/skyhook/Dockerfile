FROM nginx:1.30-alpine

RUN apk add --no-cache openssl \
	&& mkdir -p /etc/nginx/tls \
	&& openssl req -x509 -newkey rsa:2048 -noenc -days 3650 \
		-subj "/CN=skyhook.sonarr.tv" -addext "subjectAltName=DNS:skyhook.sonarr.tv" \
		-keyout /etc/nginx/tls/key.pem -out /etc/nginx/tls/cert.pem

COPY default.conf /etc/nginx/conf.d/default.conf
COPY v1/ /usr/share/nginx/html/v1/
