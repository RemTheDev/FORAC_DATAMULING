# DataMulingFilePackager

## Description de l'application

DataMulingFilePackager est une application qui sert à empaqueter les fichiers transmis au Raspberry Pi en paquets sécurisé. Il est conçu pour être lancé périodiquement.

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

Copier le dossier linux-arm64 de la cible de publication sur l'ordinateur sur lequel l'application doit être déployée.


## Étape 3 — Configuration

Pour configurer l'application, il suffit de modifier le fichier appsettings.json. Voici les paramètres à définir:
- FileInformationDbPath : Emplacement du fichier de la base de donnée.
- LogFolderLocation : Dossier dans lequel les journaux seront sauvegardé.
- DataFilesPath : Emplacement du dossier partagé sur le Raspberry Pi.
- PackageFilesPath : Chemin du dossier où les paquets prêts à être transmit seront déposé.
- PackageHeaderFileName : Nom du fichier d'en-tête des paquets.
- MaxPackageAgeDays : Âge maximum des paquets. Les paquets plus vieux que ce nombre de jours sont supprimés.

## Étape 4 — Déployer le service

Pour le déploiement du service, veuiller vous référer à la section 7.2 de la documentation de déploiement du Raspberry Pi dans FORAC_DATAMULING/RaspberryPi/readme.md.

## Étape 5 — Vérification du fonctionnement

Après le lancement :

- Les fichiers déposé dans le dossier partagé du Raspberry Pi devrait être empaqueté dans le dossier "PackageFilesPath", configuré à l'étape 3, aux 5 minutes.

## Journalisation
Les journaux du service peuvent être consulté avec la commande suivante:

```bash
journalctl -b -u datamulingTCPServer.service
```