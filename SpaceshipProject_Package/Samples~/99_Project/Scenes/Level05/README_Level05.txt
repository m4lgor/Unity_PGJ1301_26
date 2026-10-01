README - Level 05 : Perception et contournement


Situation :

Un obstacle se dresse entre vous et le Goal.

Et surtout : le Goal n'est plus detecte automatiquement. TargetPosition ne se met plus
a jour toute seule, elle reste sur la position de depart. Personne ne vous dira plus ou
aller.

Le vaisseau tourne toujours sur lui-meme, comme aux Niveaux 02 et 03.


Objectif :

Trouver le Goal par vous-meme avec une query, puis contourner l'obstacle pour
l'atteindre - et Y RESTER 1 SECONDE, comme au Niveau 03 et 04.

NOUVEAU : l'obstacle ne pardonne pas.

Le Goal arrive avec une plaque posee en travers, toujours entre vous et lui. Si votre
coque la touche, le Goal disparait : un nouveau apparait ailleurs.

C'est la regle des etoiles du Niveau 04, appliquee a un mur qui ne bouge pas. Le
compteur d'impacts est en bas a gauche.

Consequence directe : il ne suffit plus de viser le Goal et de pousser. Il faut savoir
AVANT de vous engager si la ligne droite est libre.


Details :

Il vous faut une query pour trouver le Goal : Raycast, SphereCast, OverlapSphere, celle
que vous voulez. Le Goal est le seul objet portant un GoalComponent - filtrez avec
GetComponent<GoalComponent> ce que votre query vous renvoie.

Une fois trouve, c'est a VOUS de renseigner TargetPosition.

Quand le Goal est atteint, SpaceshipControllerBase.OnGoalReached est appelee et un
nouveau Goal apparait ailleurs. Votre ancienne position de Goal ne vaut plus rien :
relancez une detection. OnGoalSpawned et OnGoalDestroyed sont la si vous preferez une
logique evenementielle.
