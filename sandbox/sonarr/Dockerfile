FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim

ARG SONARR_VERSION
ARG SONARR_SHA256

RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl sqlite3 \
	&& rm -rf /var/lib/apt/lists/*

RUN : "${SONARR_VERSION:?compose.yaml passes SONARR_VERSION}" "${SONARR_SHA256:?compose.yaml passes SONARR_SHA256}" \
	&& curl -fsSL --retry 3 -o /tmp/sonarr.tar.gz \
		"https://github.com/Sonarr/Sonarr/releases/download/v${SONARR_VERSION}/Sonarr.main.${SONARR_VERSION}.linux-x64.tar.gz" \
	&& echo "${SONARR_SHA256}  /tmp/sonarr.tar.gz" | sha256sum -c - \
	&& mkdir -p /opt/sonarr/bin \
	&& tar -xzf /tmp/sonarr.tar.gz -C /opt/sonarr/bin --strip-components=1 --no-same-owner \
	&& rm /tmp/sonarr.tar.gz \
	&& printf 'UpdateMethod=Docker\nPackageVersion=%s\nPackageAuthor=PoshSonarr sandbox\n' "${SONARR_VERSION}" > /opt/sonarr/package_info

# /tv-temp is an empty folder beside /tv for tests to add as a root folder and remove, since Sonarr accepts a root
# folder only when the folder exists and Sonarr can write to it. It isn't a volume, so recreating the container empties
# it.
RUN groupadd -g 1000 sonarr \
	&& useradd -u 1000 -g sonarr -d /config -M -s /usr/sbin/nologin sonarr \
	&& mkdir -p /config /tv /tv-temp \
	&& chown sonarr:sonarr /config /tv /tv-temp

USER sonarr
EXPOSE 8989
ENTRYPOINT ["/opt/sonarr/bin/Sonarr", "-nobrowser", "-data=/config"]
