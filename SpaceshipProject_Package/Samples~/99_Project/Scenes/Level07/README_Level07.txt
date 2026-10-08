README - Level 07 : La course du tunnel


Situation :

Un grand tunnel rectangulaire, de la meme taille d'un bout a l'autre, trois
sections, et un quai d'arrimage tout au bout.
Rien de nouveau a apprendre : tout ce que vous avez ecrit depuis le Niveau 00 sert ici,
en meme temps.


Objectif :

Traverser le tunnel en passant par les 6 checkpoints (des Goals, comme avant), puis vous
arrimer au quai - le dernier Goal - et Y RESTER 6 SECONDES.

Le Goal n'est pas detecte automatiquement : a vous de le trouver avec une query, et de
renseigner TargetPosition. Quand un checkpoint est valide, OnGoalReached est appelee et le
suivant apparait plus loin. Le compteur, en bas au centre, donne la section, le checkpoint,
le nombre de retours et le temps.


Les trois sections :

1. LES MURS (queries - Niveau 05)

   Quelques petits murs inclines se tiennent sur la route des deux premiers checkpoints.
   Visez le checkpoint tout droit, et vous finissez dans un mur.

   Il faut savoir AVANT de vous engager ce qu'il y a devant : Raycast, SphereCast (votre
   vaisseau a un rayon, pas un Raycast !), OverlapSphere... et choisir de quel cote contourner.

2. LE CHAMP D'ASTEROIDES (sphere-sphere - Niveau 04)

   Des etoiles derivent vers vous, de la droite vers la gauche, a 2,5 - 4,5 m/s. Certaines
   sont lancees droit sur votre vaisseau. Comme au Niveau 04, elles n'ont pas de collider
   qui les repousse : a vous de detecter le contact (Manifold) et de les devier avec
   StarComponent.Deflect.

   Nouveau : les etoiles ont maintenant un collider TRIGGER. Il ne pousse rien, mais une
   query les VOIT. Au lieu de parcourir StarField.Instance.Stars, vous pouvez chercher les
   etoiles proches avec un OverlapSphere. (La liste existe toujours, si vous preferez.)

3. LA TEMPETE SOLAIRE (joints - Niveau 06)

   Le vent solaire souffle par rafales : calme 3 s, avertissement 1,5 s, rafale 2,5 s. Il
   est plus fort que vos moteurs, et il vous pousse loin du quai - vers l'arriere, ou dans
   un mur. OnGustWarning vous donne la direction.

   Accrochez-vous : un SpringJoint, CONNECTE au Rigidbody d'un mur (tous les murs en ont
   un), connectedAnchor en espace LOCAL de ce mur, maxDistance jamais plus courte que
   votre distance a l'ancre. Memes regles qu'au Niveau 06 : SpringJoint seulement, connectedBody
   obligatoire, une corde trop courte CASSE.

   Quelques etoiles traversent aussi la section, et le quai se tient avec une corde : devier
   une etoile pendant qu'on est accroche, c'est le melange.


Les chocs :

Tous les murs sont JAUNES : les petits murs inclines et les bords du tunnel. Un choc contre
l'un d'eux plus rapide que 1,5 m/s, ou une etoile qui atteint la coque, vous renvoie au
DERNIER checkpoint valide, a l'arret, a l'endroit, et SANS corde (le niveau supprime vos
joints). SpaceshipControllerBase.OnSentBackToCheckpoint vous previent : oubliez ce que vous
teniez. Un choc plus lent est gratuit.

Tant que le premier checkpoint n'est pas valide, vous revenez au depart.


Details :

- Les murs sont des BoxCollider, avec un Rigidbody kinematic.

- Ne perdez pas de vue la regle du projet : le vaisseau ne bouge que par RocketEngine.Thrust
  et par vos joints. Jamais de transform.position ni de AddForce.
