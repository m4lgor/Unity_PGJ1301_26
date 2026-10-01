LREADME - Level 06 : Vent solaire


Situation :

Un vent solaire souffle sur l'arène, par rafales. Il pousse le vaisseau LOIN du Goal.

Objectif :

Rester dans le Goal 6 secondes, malgre le vent. 
Le vent est plus fort que vos moteurs. 
Une rafale arrivera forcément lorsque vous êtes dans le GoalComponent.

Il ne reste qu'une solution : vous accrocher. 
Créer un SpringJoint entre votre vaisseau et un mur pour vous tenir, et résister au vent ! 

Methode :

Le vent suit un cycle : calme, avertissement (1 s), rafale (2,5 s), calme...

Pendant l'avertissement la direction est connue, et elle ne change plus avant la fin de la
rafale. 

SpaceshipControllerBase.OnGustWarning(direction, force, secondsUntilGust) vous la
donne. 
OnGustStarted et OnGustEnded vous disent quand la rafale commence et se termine.

Vous devrez donc créer un Joint sur votre Spaceship, qui le tiendra accroché pendant toute 
la durée de la rafale.

REGLES :
Votre joint doit etre CONNECTE a un Rigidbody (connectedBody). On ne s'accroche pas dans
le vide. Les murs de l'arène portent chacun un Rigidbody kinematic pour cela. Un joint sans
connectedBody est supprimé par le niveau, avec une erreur dans la console.

Un Joint plus courte que la distance actuelle tire le vaisseau d'un coup, et tres fort :
au-dela de 1 m d'ecart avec maxDistance, la corde CASSE (le niveau la supprime, avec une
erreur dans la console).

Details :

- Utilisez une Query pour trouver un point d'accroche pour votre corde.
  
- connectedBody : le Rigidbody du mur (hit.rigidbody). Obligatoire, voir la regle.

- connectedAnchor est LOCAL a ce Rigidbody, pas au monde : hit.point est une position monde,
  il faut la convertir avec InverseTransformPoint, sur le Transform de votre connected body.
  
- Votre Spring Joint ne doit pas avoir une distance plus courte que votre distance avec le point d'accroche.
  Voir regles.
  
- anchor est local au vaisseau. Le centre de masse du vaisseau est l'origine de son Transform.

- autoConfigureConnectedAnchor doit etre a false.


