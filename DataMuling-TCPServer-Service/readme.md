# DataMuling-TCPServer-Service

## Description de l'application

DataMuling-TCPServer-Service est un service à installer sur le Raspberry Pi qui permet à un autre appareil de communiquer avec le Raspberry Pi via une connexion TCP. Cette connexion permet d'obtenir la liste des packets disponible au téléchargement, télécharger ces packets, confirmer que le transfert d'un packet s'est complété et téléverser des fichiers à empaqueter sur le Raspberry Pi.

---

# Déploiement

## Prérequis

Avant de déployer l'application, assurez-vous d'avoir :

- Le SDK .NET 10 installé
- Visual Studio ou le CLI .NET pour publier


## Étape 1 — Publier l'application

Dans Visual Studio, publier le projet DataMuling-TCPServer-Service.csproj (clique-droit > publier). Choisissez les configurations suivantes :

- Type de cible : Dossier
- Configuration : Release
- Infrastructure cible : net10.0
- Runtime cible : linux-arm64
- Mode de déploiement : Autonome
- Options de publication : Produire un seul fichier

Si vous utilisez le CLI .NET, veuillez vous référer à la documentation pour publier le projet en utilisant les configurations précédentes : https://learn.microsoft.com/fr-ca/dotnet/core/tools/dotnet-publish

## Étape 2 — Copier le dossier généré

Copier le dossier linux-arm64 de la cible de publication sur l'ordinateur sur lequel le service doit être déployé.


## Étape 3 — Configuration

Pour configurer l'application, il suffit de modifier le fichier appsettings.json. Voici les paramètres à définir:
- FileInformationDbPath : Emplacement du fichier de la base de donnée.
- AvailablePackagesFilesPath : Chemin du dossier qui contient les paquets prêts à être transmit.
- SharedFileInputPath : Emplacement du dossier partagé sur le Raspberry Pi.
- Port : Port utilisé pour la connexion TCP.

## Étape 4 — Déployer le service

Pour le déploiement du service, veuiller vous référer à la section 7.1 de la documentation de déploiement du Raspberry Pi dans FORAC_DATAMULING/RaspberryPi/readme.md.

## Étape 5 — Vérification du fonctionnement

Après le lancement :

- Vérifier le satut du service avec la commande suivante :

```bash
systemctl status datamulingTCPServer.service
```

## Journalisation
Les journaux du service peuvent être consulté avec la commande suivante:

```bash
journalctl -b -u datamulingTCPServer.service
```