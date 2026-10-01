README - Level 03 : Rester dans la zone

Situation :

Le niveau est identique au Niveau 02. Votre Spaceship n'a toujours pas ses rotations
verrouillees autour de l'axe Z, il apparait avec une rotation aleatoire, et il recoit
une impulsion angulaire au demarrage : il tourne deja sur lui-meme des la premiere
frame, et rien ne freine cette rotation.

Une seule chose change, et c'est tout le niveau.


NOUVEAU : le temps dans la zone.

Toucher le Goal ne suffit plus. Il faut RESTER dedans 1 seconde d'affilee pour
qu'il soit valide. Si vous ressortez avant la fin, le compteur repart de zero.

Le Goal vous le montre : il devient de plus en plus net et solide pendant que vous
tenez la position, et il retourne a son clignotement habituel des que vous sortez.

Vous n'avez rien a coder pour ce chronometre, il est gere par le Goal lui-meme.


Objectif :

Atteindre le AutoFindGoal Gameobject avec votre Spaceship, ET Y RESTER 1 seconde.


Pourquoi c'est plus dur qu'il n'y parait :

Jusqu'ici, traverser le Goal a pleine vitesse suffisait. Plus maintenant. Or rien ne
vous ralentit dans l'espace (premiere loi de Newton) : la seule facon de vous arreter
est de pousser dans l'autre sens, et surtout de commencer assez tot.

La distance dont vous avez besoin pour freiner vaut :

    distance de freinage = v * v / (2 * a)      avec    a = F / m

Notez le carre : deux fois plus vite, c'est QUATRE fois plus de distance pour vous
arreter. Si vous arrivez trop vite, aucun freinage de derniere seconde ne vous sauvera.

C'est exactement ce que vous avez travaille dans le TP Rendezvous
(Samples/03_LawsOfMotion/TP_Rendezvous). Reprenez votre raisonnement.


Details :

Continuez dans MyCustomSpaceshipController.cs, le meme fichier que depuis le
Niveau 00.

Comme au Niveau 02, votre Spaceship tourne en permanence : la direction dans laquelle
pousse un moteur change a chaque frame. Ne mettez pas en cache le resultat de
GetPushDirection(), relisez-le a chaque FixedUpdate.

Il vous faut donc, a chaque FixedUpdate :
  - savoir dans quelle direction vous devez pousser (vers le Goal, ou dans l'autre
    sens si vous devez freiner),
  - retrouver le moteur qui pousse effectivement dans cette direction MAINTENANT,
  - et lui donner la bonne puissance.

Le Goal est toujours detecte automatiquement : TargetPosition est remplie pour vous,
vous n'avez aucune query a faire. Cela viendra au Niveau 04.

La position et la vitesse de votre vaisseau sont dans _Rigidbody.position et
_Rigidbody.linearVelocity. Sa masse est dans _Rigidbody.mass, et la poussee maximale
d'un moteur dans RocketEngine.GetMaxPower(). Vous avez tout pour calculer a = F / m :
ne codez pas l'acceleration en dur.


ATTENTION :

SpaceshipControllerBase.Update() n'est PAS virtual.
Si vous declarez votre propre Update() dans votre classe, vous masquez celui de la
classe de base, UpdateTarget() ne tourne plus, et TargetPosition reste bloquee sur la
position de depart. Aucune erreur ne s'affiche.

C'est pour cela que FixedUpdate() est deja ecrit pour vous : mettez-y votre code, et
ne declarez pas d'Update().
