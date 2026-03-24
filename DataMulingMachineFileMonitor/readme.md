# DataMulingMachineFileMonitor

## Description de l'application

Cette application est conçue pour être installée sur l'ordinateur d'une abatteuse, en tant que service Windows. Son rôle est de surveiller un dossier dans lequel les fichiers de tête d'abatage sont déposés par le logiciel du fabricant et de copier ces fichiers dans le répertoire partagé du Raspberry Pi. Pour ce faire, l'ordinateur doit être connecté au réseau Wi-Fi disponible via le Raspberry Pi. Le service s'assure aussi de cette connectivité au réseau Wi-Fi.

---

# Déploiement

## Prérequis

Avant de déployer l'application, assurez-vous d'avoir :

- Le SDK .NET 10 installé
- Visual Studio ou le CLI .NET pour publier


## Étape 1 — Publier l'application

Dans Visual Studio, publier le projet DataMulingMachineFileMonitor.csproj (clique-droit > publier). Choisissez les configurations suivantes :

- Type de cible : Dossier
- Configuration : Release
- Infrastructure cible : net10.0
- Runtime cible : win-x64 (Modifier selon l'ordinateur sur lequel le service sera installé)
- Mode de déploiement : Autonome
- Options de publication : Produire un seul fichier

Si vous utilisez le CLI .NET, veuillez vous référer à la documentation pour publier le projet en utilisant les configurations précédentes : https://learn.microsoft.com/fr-ca/dotnet/core/tools/dotnet-publish

## Étape 2 — Copier le dossier généré

Copier le dossier win-x64 de la cible de publication sur l'ordinateur sur lequel le service doit être déployé.


## Étape 3 — Configuration

Pour configurer l'application, il suffit de modifier le fichier appsettings.json. Voici les paramètres à définir:
- MachineFileInputFolder : Le dossier dans lequel les fichiers de tête d'abatage seront déposés par le logiciel du manufacturier.
- DataMulingSharedFolder : L'emplacement réseau du dossier partagé du Raspberry Pi.
- DataMulingNetworkProfileName : Le nom du profil Wi-Fi du Raspberry Pi.
- FileCopyPollingIntervalSeconds : Fréquence de la copie des fichiers dans le dossier partagé en secondes.
- AutoReconnectToDataMulingNetwork : Permet de se connecter automatiquement au réseau Wi-Fi du Raspberry Pi quand la valeur est "true".
- DataMulingSharedFolderUsername : Le nom d'utilisateur du Raspberry Pi. Déterminé lors du déploiement du Raspberry Pi.
- DataMulingSharedFolderPassword : Le mot de passe de l'utilisateur du Raspberry Pi. Déterminé lors du déploiement du Raspberry Pi.
- DataMulingNetworkSSID : SSID du réseau Wi-Fi du Raspberry Pi. C'est le nom du réseau qui s'affiche dans la liste des réseaux disponibles. Déterminé lors du déploiement du Raspberry Pi.
- DataMulingNetworkPassword : Mot de passe du réseau Wi-Fi. Déterminé lors du déploiement du Raspberry Pi.

## Étape 4 — Déployer le service Windows

Déployer l'application en tant que service Windows en suivant les étapes suivantes:
- Sur l'ordinateur sur lequel le service doit être déployé, lancer une invite de commande en tant qu'administrateur.
- Si le service existe déjà, lancer les commandes suivante pour supprimer l'installation précédente :

```bash
sc.exe stop DataMulingMachineFileMonitor
sc.exe delete DataMulingMachineFileMonitor
```

- Lancer cette commande pour installer le service :

```bash
sc.exe create DataMulingMachineFileMonitor start=auto BinPath=<dossier>\DataMulingMachineFileMonitor.exe
```
Remplacer \<dossier\> par le chemin du dossier copié à l'étape 2. Ex. :

```bash
sc.exe create DataMulingMachineFileMonitor start=auto BinPath=C:\MachineFileMonitor\v1\win-x64\DatamulingMachineFileMonitor.exe
```

- Ouvrir la fenêtre des service windows en cherchant "Services" dans la barre de recherche.

- Lancer le service nommé "DatamulingMachineFileMonitor"

## Étape 5 — Vérification du fonctionnement

Après le lancement :

- Vérifier que la machine se connecte bien au Wi-Fi du Raspberry Pi.
- Déposer un fichier test dans le répertoire surveillé.
- Confirmer que le fichier est bien copié sur le répertoire partagé du Raspberry Pi.
- En cas de problème, vérifier les journaux.

## Journalisation

Les journaux sont disponibles dans le EventViewer de Windows.