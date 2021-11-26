#!/bin/bash

KEY_FILE="$1.key"
CRT_FILE="$1.crt"
PFX_FILE="$1.pfx"
FQDN_REPLACE="$1"
PASSWORD=Password.1

if test -z "$1" ; then
	FQDN_REPLACE="kano.api"
fi

sed -e "s/\_FQDN\_/$FQDN_REPLACE/" ./kano.cert.conf > kano.cert.fqdn
cp kano.cert.conf kano.cert.tmp
mv kano.cert.fqdn kano.cert.conf
openssl req -x509 -sha256 -nodes -days 3650 -config kano.cert.conf -newkey rsa:2048 -keyout "$KEY_FILE" -out "$CRT_FILE"

if test -z "$2" ; then
	PASSWORD=$2
fi
# openssl pkcs12 -export -out "$PFX_FILE" -inkey "$KEY_FILE" -in "$CRT_FILE" -password pass:$PASSWORD
openssl pkcs12 -export -out "$PFX_FILE" -inkey "$KEY_FILE" -in "$CRT_FILE"

mkdir -p "$1"
mv $1.* "./$1"
mv kano.cert.tmp kano.cert.conf
