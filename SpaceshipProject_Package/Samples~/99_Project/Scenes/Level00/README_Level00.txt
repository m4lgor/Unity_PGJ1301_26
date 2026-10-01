∩╗┐README - Level 00 : Premier propulseur

Situation : 

Le vaisseau est immobile au centre de l'ar├¿ne, sans gravit├⌐.
Il ne tourne pas : sa rotation est verrouill├⌐e et son orientation ne change jamais.

Le Goal appara├«t toujours au m├¬me endroit, droit au-dessus du vaisseau.


Objectif : 

Atteindre le Goal en utilisant un seul propulseur.


D├⌐tails :

Votre script MyCustomSpaceshipController est d├⌐j├á ├⌐crit et d├⌐j├á ajout├⌐ au Spaceship de la sc├¿ne.
Vous n'avez rien ├á brancher : ouvrez MyCustomSpaceshipController.cs et remplissez FixedUpdate().
C'est le m├¬me fichier pour tous les niveaux, il grandira au fil du projet.

Le vaisseau a 4 RocketEngine. Un seul pousse vers le haut.
Utilisez RocketEngine.GetPushDirection() pour trouver lequel, puis appelez
RocketEngine.Thrust(Power) sur celui-l├á. Les autres restent ├⌐teints.

La liste des moteurs vous est donn├⌐e dans _RocketEngines.
La position du Goal vous est donn├⌐e dans TargetPosition, il est d├⌐tect├⌐ automatiquement.

Vous ne devez ni d├⌐placer le Transform, ni appliquer de force directement sur le Rigidbody.

Ce niveau ne demande qu'une chose : faire bouger le vaisseau dans la bonne direction.
Ne cherchez pas encore ├á vous arr├¬ter proprement sur le Goal, c'est le Niveau 01.


ATTENTION : 

SpaceshipControllerBase.Update() n'est PAS virtual.
Si vous d├⌐clarez votre propre Update(), vous masquez celui de la classe de base,
UpdateTarget() ne tourne plus, et TargetPosition reste bloqu├⌐e sur la position de d├⌐part.
Aucune erreur ne s'affiche.

C'est pour cela que FixedUpdate() est d├⌐j├á ├⌐crit pour vous : mettez-y votre code, et
ne d├⌐clarez pas d'Update().
