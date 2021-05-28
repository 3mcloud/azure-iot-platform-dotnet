{{/* vim: set filetype=mustache: */}}
{{/*
Expand the name of the chart.
*/}}
{{- define "platform.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "platform.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "platform.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Common labels
*/}}
{{- define "platform.labels" -}}
helm.sh/chart: {{ include "platform.chart" . }}
{{ include "platform.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{/*
Selector labels
*/}}
{{- define "platform.selectorLabels" -}}
app.kubernetes.io/name: {{ include "platform.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}

{{/*
Create the name of the service account to use
*/}}
{{- define "platform.serviceAccountName" -}}
{{- if .Values.serviceAccount.create -}}
    {{ default (include "platform.fullname" .) .Values.serviceAccount.name }}
{{- else -}}
    {{ default "default" .Values.serviceAccount.name }}
{{- end -}}
{{- end -}}

{{/*
Determine the Let's Encrypt environment URL
*/}}
{{- define "mmm-iot-platform-ingress.letsEncryptEnvironmentUrl" -}}
{{- if eq .Values.letsEncryptEnvironment "prod" -}}
https://acme-v02.api.letsencrypt.org/directory
{{- else if eq .Values.letsEncryptEnvironment "staging" -}}
https://acme-staging-v02.api.letsencrypt.org/directory
{{- else -}}
{{- fail "Error: .Values.letsEncryptEnvironment must be either staging or prod" -}}
{{- end -}}
{{- end -}}

{{/*
Determine the ingress name
*/}}
{{- define "mmm-iot-platform-ingress.ingressName" -}}
ingress-{{ required "Error: missing required value .Values.subdomain" .Values.subdomain }}
{{- end -}}

{{/*
Determine the issuer name
*/}}
{{- define "mmm-iot-platform-ingress.issuerName" -}}
issuer-{{ required "Error: missing required value .Values.subdomain" .Values.subdomain }}
{{- end -}}

{{/*
Determine the fully-qualified domain name
*/}}
{{- define "mmm-iot-platform-ingress.fqdn" -}}
{{ required "Error: missing required value .Values.subdomain" .Values.subdomain }}.{{ .Values.domain }}
{{- end -}}