This work serves as a proof of concept, showcasing my vision of the most informative way to present disk information.

# Details

The idea is simple — divide the occupied/free space bar into blocks of a fixed byte size (e.g., 100 GB). This way, both used and remaining space are immediately clear at a glance. Unlike my approach, the "classic" solid bar representation provides no absolute information about how much space is used or free; it only shows a relative percentage, which is of little practical use.

## Solid bar (classical widespread solution)
![image](https://github.com/user-attachments/assets/31ae71f1-dfed-4253-a69a-0234d7b9fb0b)

* Total drive space can be known only by looking on the numbers
* Remaining space shown in percent from total drive space, it's not easy to estimate absolute remaining/occupied space (how much GB left)

## Bar with fixed byte size blocks (my proposed solution)
![image](https://github.com/user-attachments/assets/b6fdb580-59b4-4058-b98d-ffc49c4c19dd)

* Total drive space can be easily visually estimated (only if it's larger than block size).
* Remaining space still shown in percent¹ and it's easy to estimate absolute remaining/occupied space (how much GB left) visually.

  ¹ A bit inaccurate, becouse I didn't deal with spacings in partial block fill calculations, but it's not the idea's problem
