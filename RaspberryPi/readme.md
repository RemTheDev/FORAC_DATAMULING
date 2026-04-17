# DataMuling – Déploiement et configuration du Raspberry Pi

Ce document décrit les étapes de déploiement et de configuration du Raspberry Pi utilisé dans le prototype **DataMuling**.

À ce stade, cette documentation couvre uniquement :

- l’installation du système sur le Raspberry Pi ;
- la configuration du Raspberry Pi comme **point d’accès Wi-Fi local** ;
- la mise en place d’un **dossier partagé Samba** pour le dépôt de fichiers ;
- la création de l’arborescence locale pour les données et les journaux ;
- le déploiement du **serveur TCP** ;
- la configuration de l’exécution périodique du **File Packager**.

Une image d'un Raspberry Pi déployé peut être téléchargée ici : 
https://1drv.ms/u/c/e1bc7463ab8d178d/IQCMy3tRbIgURpDZgh4Jm8uEAVcoijG4mzvBSonb6v5TY0c?e=lmYYjR

## Vue d’ensemble

Dans le prototype actuel :

1. une application se connecte au réseau local créé par le Raspberry Pi afin d’y déposer des fichiers ;
2. le Raspberry Pi prépare des **paquets sécurisés** à partir de ces fichiers ;
3. un téléphone cellulaire peut ensuite se connecter au réseau local du Raspberry Pi et télécharger ces paquets via une connexion TCP/IP.
4. Un **module UPS** s'assure que le raspberry Pi reste alimenter et qu'il s'éteint correctement si un manque de courrant se produit.

> **Note**  
> Cette documentation décrit uniquement la partie **déploiement et configuration du Raspberry Pi**.  
> La documentation applicative et les détails du format des paquets seront ajoutés ultérieurement.  
> Cette documentation ne couvre pas la configuration du **module UPS**.

---

## Prérequis

### Matériel

- un **Raspberry Pi Zero 2W ou autre Raspberry Pi ayant une carte réseau** ;
- une carte **microSD** ;
- un ensemble **clavier / souris / écran** pour la configuration initiale ;
- une connexion Internet temporaire pour l’installation des paquets ;
- un **module UPS**, comme une PiJuice par exemple
- une clé USB (optionnelle)

### Logiciels

- **Raspberry Pi Imager** ;
- **Raspberry Pi OS Lite (64-bit)** ;
- accès administrateur au Raspberry Pi (`sudo`).

---

## 1. Installation initiale du système

Utiliser **Raspberry Pi Imager** pour installer **Raspberry Pi OS Lite (64-bit)** sur la carte microSD.

Lors de la préparation de l’image :

- choisir un **hostname** ;
- définir la **localisation** ;
- créer un **utilisateur / mot de passe** ;
- configurer un accès **Wi-Fi temporaire** ;
- activer **SSH**.

Ensuite :

1. éjecter la carte microSD ;
2. l’installer dans le Raspberry Pi ;
3. démarrer le Raspberry Pi ;
4. compléter la configuration initiale ;
5. vérifier que la connectivité Internet fonctionne.

Exemple :

```bash
ping google.com
```

Pour valider ou ajuster la configuration de base :

```bash
sudo raspi-config
```

Pour exécuter les commandes suivantes en tant que superutilisateur :

```bash
sudo -Es
```

---

## 2. Mise à jour du système et installation des paquets requis

Mettre à jour le système :

```bash
apt-get update
apt-get upgrade
```

Installer les paquets requis :

```bash
# Pour utiliser hwclock
apt-get install util-linux-extra

# Pour le partage de fichiers
apt-get install samba

# Pour le mécanisme réseau local
apt install --download-only libnss-resolve
```

---

## 3. Remplacement de la configuration réseau classique par `systemd-networkd`

Le déploiement proposé retire les composants réseau classiques (`ifupdown`, `dhcpcd`, `network-manager`, etc.) et utilise `systemd-networkd` avec `systemd-resolved` à la place.

### 3.1 Désactiver les composants réseau classiques

```bash
systemctl daemon-reload
systemctl disable --now ifupdown dhcpcd dhcpcd5 isc-dhcp-client isc-dhcp-common rsyslog
apt --autoremove purge ifupdown dhcpcd dhcpcd5 isc-dhcp-client isc-dhcp-common rsyslog
rm -r /etc/network /etc/dhcp
apt remove --purge network-manager
```

### 3.2 Activer `systemd-networkd` et `systemd-resolved`

```bash
systemctl disable --now avahi-daemon libnss-mdns
apt --autoremove purge avahi-daemon
apt install libnss-resolve
ln -sf /run/systemd/resolve/stub-resolv.conf /etc/resolv.conf
apt-mark hold avahi-daemon dhcpcd dhcpcd5 ifupdown isc-dhcp-client isc-dhcp-common libnss-mdns openresolv raspberrypi-net-mods rsyslog
systemctl enable systemd-networkd.service systemd-resolved.service
```

---

## 4. Configuration du Raspberry Pi comme point d’accès Wi-Fi

Le Raspberry Pi est configuré pour créer un réseau local sans fil nommé `RPiZero1`, protégé par mot de passe.

### 4.1 Créer la configuration `wpa_supplicant`

Créer le fichier `/etc/wpa_supplicant/wpa_supplicant-wlan0.conf` :

```bash
cat > /etc/wpa_supplicant/wpa_supplicant-wlan0.conf <<EOF
country=CA
ctrl_interface=DIR=/var/run/wpa_supplicant GROUP=netdev
update_config=1

network={
    ssid="RPiZero1"
    mode=2
    frequency=2437
    key_mgmt=WPA-PSK
    proto=RSN WPA
    psk="********"
}
EOF
```

Appliquer les permissions et activer le service :

```bash
chmod 600 /etc/wpa_supplicant/wpa_supplicant-wlan0.conf
systemctl disable wpa_supplicant.service
systemctl enable wpa_supplicant@wlan0.service
rfkill unblock wlan
```

### 4.2 Configurer l’interface `wlan0`

Créer le fichier `/etc/systemd/network/08-wlan0.network` :

```bash
cat > /etc/systemd/network/08-wlan0.network <<EOF
[Match]
Name=wlan0

[Network]
Address=192.168.4.1/24
MulticastDNS=yes
DHCPServer=yes
EOF
```

---

## 5. Mise en place du partage Samba

Un dossier partagé Samba nommé `DataMuling` est utilisé pour déposer les fichiers sur le Raspberry Pi lorsqu’un appareil est connecté au point d’accès.

### 5.1 Créer l’utilisateur Samba et le dossier partagé

```bash
smbpasswd -a forac #Nom d'utilisateur local de la pi
mkdir /srv/share
mkdir /srv/share/DataMuling
chown forac /srv/share/DataMuling
```

### 5.2 Sauvegarder la configuration Samba

```bash
cp /etc/samba/smb.conf /etc/samba/smb_backup.conf
nano /etc/samba/smb.conf
```

Ajouter à la fin du fichier :

```ini
[DataMuling]
path = /srv/share/DataMuling
valid users = forac
read only = no
```

Puis redémarrer Samba et valider la configuration :

```bash
service smbd restart
testparm
```

Redémarrer le Raspberry Pi :

```bash
reboot
```

Une fois connecté au point d’accès Wi-Fi du Raspberry Pi, le partage devient accessible à l’adresse :

```text
\\192.168.4.1\DataMuling
```

avec l’utilisateur local configuré (`forac` dans cet exemple).

---

## 6. Création de l’arborescence de données et de journaux

Créer les dossiers nécessaires :

```bash
mkdir /var/local/datamuling
mkdir /var/local/datamuling/packages
mkdir /var/log/DataMulingLogs
mkdir /var/log/DataMulingLogs/FilePackager
```

Donner les droits d’écriture :

```bash
chmod -R 777 /var/log/DataMulingLogs
chmod -R 777 /var/local/datamuling
```

Ces emplacements sont utilisés pour les données locales, les paquets générés et les journaux d’exécution.

---

## 7. Déploiement des exécutables

Les versions publiées (*releases*) du **serveur TCP** et du **File Packager** doivent être copiées sur le Raspberry Pi, soit à partir d’un autre ordinateur via le partage Samba, soit à l’aide d’une clé USB.

### 7.1 Serveur TCP

```bash
mkdir /usr/local/bin/DataMulingTCPServerService
cp <chemin_du_fichier_de_bd_sqlite> /var/local/datamuling/
cp -r <chemin_du_dossier_de_la_release_TCPServer(linux-arm64)> /usr/local/bin/DataMulingTCPServerService/
```

Au besoin, mettre à jour `appsettings.json`.

### 7.2 File Packager

```bash
mkdir /usr/local/bin/DataMulingFilePackager
cp -r <chemin_du_dossier_de_la_release_FilePackager(linux-arm64)> /usr/local/bin/DataMulingFilePackager
```

Au besoin, mettre à jour `appsettings.json`.

À ce point, voici ce que devrait contenir /usr/local/bin/:
```bash
|-DataMulingTCPServerService
 | |-linux-arm64
 | | |-DataMulingPackageDB.pdb
 | | |-DataMuling-TCPServer-Service.pdb
 | | |-DataMuling-TCPServer-Service
 | | |-libe_sqlite3.so
 | | |-appsettings.json
 |-DataMulingFilePackager
 | |-linux-arm64
 | | |-DataMulingPackageDB.pdb
 | | |-DataMulingFilePackager.pdb
 | | |-DataMulingFilePackager
 | | |-FORAC.Utility.pdb
 | | |-libe_sqlite3.so
 | | |-appsettings.json
```

---

## 8. Configuration du service `systemd` pour le serveur TCP

Créer le fichier suivant :

```bash
cd /etc/systemd/system/
nano datamulingTCPServer.service
```

Contenu du fichier :

```ini
[Unit]
Description=TCP Server for DataMuling

[Service]
Type=notify
WorkingDirectory=/usr/local/bin/DataMulingTCPServerService/linux-arm64/
ExecStart=/usr/local/bin/DataMulingTCPServerService/linux-arm64/DataMuling-TCPServer-Service
User=forac
Group=forac
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Ensuite :

```bash
chown root /usr/local/bin/DataMulingTCPServerService/linux-arm64/DataMuling-TCPServer-Service
chmod 777 /usr/local/bin/DataMulingTCPServerService/linux-arm64/DataMuling-TCPServer-Service

systemctl daemon-reload
systemctl enable datamulingTCPServer.service
systemctl start datamulingTCPServer.service
systemctl status datamulingTCPServer.service
```

---

## 9. Exécution périodique du File Packager

Le **File Packager** est configuré pour s’exécuter toutes les 5 minutes à l’aide de `cron`.

Éditer la crontab :

```bash
crontab -e
```

Ajouter la ligne suivante :

```bash
*/5 * * * * /usr/local/bin/DataMulingFilePackager/linux-arm64/DataMulingFilePackager
```

Puis redémarrer :

```bash
reboot
```

---

## 10. Vérification et journaux

### Vérifier l’état du service TCP

```bash
systemctl status datamulingTCPServer.service
```

### Consulter les journaux du service TCP

```bash
journalctl -b -u datamulingTCPServer.service
```

### Consulter les journaux du File Packager

```bash
cat /var/log/DataMulingLogs/FilePackager/<date>_log.txt
```

---

## 11. Résumé de l’architecture locale

Une fois le déploiement terminé :

- le Raspberry Pi crée un **point d’accès Wi-Fi local** ;
- un appareil peut s’y connecter pour déposer des fichiers via le **partage Samba** ;
- les fichiers déposés sont traités localement par le **File Packager** ;
- les paquets produits sont ensuite rendus disponibles via le **serveur TCP** ;
- un téléphone cellulaire peut se connecter au même réseau local pour les télécharger.