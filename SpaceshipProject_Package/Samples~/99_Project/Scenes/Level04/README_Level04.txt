README - Level 04 : Contacts et collisions

Situation :

NOUVEAU : un mur d'etoiles barre la route, et elles n'ont PAS de Collider component.

Chaque fois qu'un Goal apparait, un mur d'etoiles se dresse en travers du chemin, a
mi-distance entre vous et lui. Les trous entre deux etoiles sont plus petits que votre
vaisseau : il n'y a pas de passage propre. Pour passer, il faut les ecarter.

Elles ont un Rigidbody, une masse et un rayon, mais pas de Collider. Unity ne sait donc
pas qu'elles existent : il ne verra pas qu'elles vous touchent, il ne les repoussera
pas, et aucun OnCollisionEnter ne sera appele.

Les etoiles du mur sont IMMOBILES. Toute la vitesse relative est donc la vôtre : c'est
votre vitesse d'approche, et elle seule, qui decide de l'impulsion qu'il vous faudra.


Objectif :

Atteindre le Goal et y rester 1 seconde. Pour cela, traverser le mur en ecartant les
etoiles, une par une.

Et surtout : au premier impact sur la coque, le Goal disparait. Il réapparait ailleurs,
derriere un nouveau mur, et tout est à refaire depuis la position ou l'impact vous a
laissé. Une approche ratee ne se rattrape pas, elle se recommence.

Le compteur en bas a gauche affiche les deux chiffres qui comptent : les etoiles que
vous avez deviees, et les impacts que vous avez pris.


Details :

Continuez dans Controller/MyCustomSpaceshipController.cs.

Ce qu'on vous donne :

    StarField.Instance.Stars        la liste des etoiles vivantes
    StarField.Instance.ShipRadius   le rayon de collision de votre vaisseau

Relisez la liste a chaque FixedUpdate, ne la mettez pas en cache : une etoile que vous
aviez gardee de la frame d'avant a pu etre detruite entre temps.

Sur chaque etoile :

    star.Radius              son rayon (la moitie de son scale, comme dans le TP)
    star.Body                son Rigidbody : masse, linearVelocity
    star.MaxDeflectImpulse   le plafond de deviation, en N.s
    star.Deflect(impulsion, pointDeContact)

Deflect est le SEUL moyen de toucher une etoile. Le Deflect échouera si vous n'êtes pas
reellement en contact avec l'étoile. La raison de l'échec est dans la console.

Ce que vous devez ecrire :

    1. La detection. C'est votre Detect() du TP_Manifold, avec A = votre vaisseau et
       B = l'étoile. Rien a changer : deux spheres, deux rayons, une soustraction.

    2. La reponse. La meme impulsion qu'au TP, avec les vraies masses :

           Vector3 relative = star.Body.linearVelocity - _Rigidbody.linearVelocity;
           float   along    = Vector3.Dot(relative, m.Normal);

           if (along > 0.0f) continue;      // elle s'eloigne deja, laissez-la

           float j = -(1.0f + e) * along / (invMoi + invElle);

           star.Deflect(m.Normal * j, m.ContactPoint);

       Attention au sens : la Normale du Manifold va de A vers B, donc de vous vers
       l'étoile. C'est la direction dans laquelle vous la poussez.

Le Deflect appliquera également une force proportionnelle à votre vaisseau. (3eme loi de Newton)

ATTENTION :

SpaceshipControllerBase.Update() n'est PAS virtual.
Si vous déclarez votre propre Update() dans votre classe, vous masquez celui de la
classe de base, UpdateTarget() ne tourne plus, et TargetPosition reste bloquee sur la
position de depart. Aucune erreur ne s'affiche.

C'est pour cela que FixedUpdate() est deja ecrit pour vous : mettez-y votre code, et
ne declarez pas d'Update().
