{{/* vim: set filetype=mustache: */}}
{{- define "service.name" -}}
  {{- required "Error: missing required value .Values.nameOverride" .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "deployment.name" -}}
  {{- required "Error: missing required value .Values.deploymentName" .Values.deployment.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "service.fullname" -}}
  {{- include "service.name" . -}}
{{- end -}}

{{- define "service.type" -}}
  {{- required "Error: missing required value .Values.type" .Values.type | trunc 63 | trimSuffix "-" -}}
{{- end -}}


{{- define "service.chart" -}}
  {{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}


{{- define "service.labels" -}}
app.kubernetes.io/name: {{ include "service.name" . }}
helm.sh/chart: {{ include "service.chart" . }}
type: {{include "service.type" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- if .Values.appVersion }}
app.kubernetes.io/version: {{ .Values.appVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{- define "service.deployment.image.repository" -}}
  {{- include "service.name" . | cat "azureiot3m/" | nospace -}}
{{- end -}} 
