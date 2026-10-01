README - Level 02 : Rotation du Spaceship

Situation : 

Votre Spaceship n'a plus ses rotations verrouillées autour de l'axe Z.
Il apparait également avec une rotation aléatoire en Z.

De plus, il reçoit maintenant une impulsion angulaire au démarrage : il tourne déjà
sur lui-même dès la première frame, et rien ne freine cette rotation.
Lors d'une collision il peut aussi se mettre à tourner davantage.

Objectif : 

Atteindre le AutoFindGoal Gameobject avec votre Spaceship.


Détails :

Étant donné que votre Spaceship tourne en permanence, il vous faudra faire attention 
à ce que les bons RocketEngine soient utilisés au bon moment selon la rotation du Spaceship.

La direction dans laquelle pousse un moteur change à chaque frame.
Ne mettez pas en cache le résultat de GetPushDirection() : relisez-le à chaque FixedUpdate.
