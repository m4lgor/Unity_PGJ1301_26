∩╗┐README - Level 01 : Premier vol

Situation : 

Votre Spaceship est immobile au centre de l'ar├¿ne, sans gravit├⌐.
Sa rotation en Z est verrouill├⌐e, mais son orientation de d├⌐part est tir├⌐e au hasard
├á chaque lancement : les moteurs ne poussent donc pas dans la m├¬me direction d'une partie ├á l'autre.

Un Goal appara├«t automatiquement dans un quadrant diff├⌐rent du v├┤tre.


Objectif : 

Atteindre le AutoFindGoal Gameobject avec votre Spaceship.


D├⌐tails :

Continuez dans MyCustomSpaceshipController.cs, le m├¬me fichier qu'au Niveau 00.
Il est d├⌐j├á ajout├⌐ au Spaceship de la sc├¿ne, vous n'avez rien ├á brancher.

Vous ne devez ni d├⌐placer le Transform, ni appliquer de force directement sur le Rigidbody.
Le seul moyen autoris├⌐ de bouger est RocketEngine.Thrust(float Power).
Le Spaceship poss├¿de 4 RocketEngine, chacun limit├⌐ ├á 20 N (cf : GetMaxPower).

La position ├á atteindre vous est donn├⌐e dans TargetPosition : dans ce niveau le Goal est
d├⌐tect├⌐ automatiquement, vous n'avez aucune query ├á faire.

Comme l'orientation de d├⌐part est al├⌐atoire, ne partez pas du principe qu'un moteur donn├⌐
pousse toujours vers le haut. Utilisez RocketEngine.GetPushDirection() pour savoir dans quelle
direction chaque moteur pousse r├⌐ellement, et choisissez ceux qui vous rapprochent de la cible.

Lorsque le Goal est atteint, SpaceshipControllerBase.OnGoalReached est appel├⌐e et un nouveau
Goal appara├«t ailleurs.


ATTENTION : 

SpaceshipControllerBase.Update() n'est PAS virtual.
Si vous d├⌐clarez votre propre Update() dans votre classe, vous masquez celui de la classe de base,
UpdateTarget() ne tourne plus, et TargetPosition reste bloqu├⌐e sur la position de d├⌐part.
Aucune erreur ne s'affiche.

C'est pour cela que FixedUpdate() est d├⌐j├á ├⌐crit pour vous : mettez-y votre code, et
ne d├⌐clarez pas d'Update().
