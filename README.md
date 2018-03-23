This is a position based dynamics particle fluid simulation running in Unity on the GPU. It uses the same math from the previous [PBD project](https://www.digital-dust.com/single-post/2017/04/09/Position-based-dynamics-in-Unity) but adapted some what to run on the GPU.

See [home page](https://www.digital-dust.com/single-post/2018/03/23/PBD-Fluid-in-Unity) for Unity package download.

The biggest difference is how finding the neighbouring particles is handled. This is more complicated on the GPU and I went with a grid hash using a Bitonic sort. Other sorting method are around (like Radix sort) and maybe faster but the Bitonic sort was simpler and works quite well. Profiling shows its not the bottle neck so a faster sort may not see much performance gain.

Its certainly not the fastest particle fluid around but can simulate 70K fluid particles and 30K boundary particles at 30fps on a GTX980 which is not too bad.

![Fluid particles](https://static.wixstatic.com/media/1e04d5_50a7e2f602a04013b9371f61bd1354bc~mv2.jpg/v1/fill/w_550,h_351,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_50a7e2f602a04013b9371f61bd1354bc~mv2.jpg)
 
The boundary conditions are handled by special particles that the fluid wont flow through. These are then added around the border. This does make it more costly than other methods but it also allows objects in the scene to interact with the fluid more easily. All you need is a method to [voxlize a mesh](https://www.digital-dust.com/single-post/2017/04/17/Mesh-voxelization-in-Unity) to particles and the fluid will flow around it using the same method.  
 
![Fluid boundary](https://static.wixstatic.com/media/1e04d5_c824c6371b6d428ebcf3637557786685~mv2.jpg/v1/fill/w_550,h_351,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_c824c6371b6d428ebcf3637557786685~mv2.jpg)
  
To render the fluid I made a compute shader that fills a 3D texture with the particles densities. That volume can then be ray traced in a shader to render the fluid. Its just a unlit shader however so has no lighting. A method would need to be added to create normals from the volume to add lighting.

![Fluid raytraced](https://static.wixstatic.com/media/1e04d5_67723414aa4341d9a23c3442b20c9b06~mv2.jpg/v1/fill/w_550,h_355,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_67723414aa4341d9a23c3442b20c9b06~mv2.jpg)

![Fluid raytraced](https://static.wixstatic.com/media/1e04d5_7882ff89a8c043128cd7475f325fe7df~mv2.jpg/v1/fill/w_550,h_355,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_7882ff89a8c043128cd7475f325fe7df~mv2.jpg)

![Fluid raytraced](https://static.wixstatic.com/media/1e04d5_b142c33546ec4fa1a748de56f3b51292~mv2.jpg/v1/fill/w_550,h_355,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_b142c33546ec4fa1a748de56f3b51292~mv2.jpg)

![Fluid raytraced](https://static.wixstatic.com/media/1e04d5_f0473e456c944033b3c214a4a57144d8~mv2.jpg/v1/fill/w_550,h_355,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_f0473e456c944033b3c214a4a57144d8~mv2.jpg)

![Fluid raytraced](https://static.wixstatic.com/media/1e04d5_3641705abb6541ebb3844f8ad767d4d3~mv2.jpg/v1/fill/w_550,h_355,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_3641705abb6541ebb3844f8ad767d4d3~mv2.jpg)

![Fluid raytraced](https://static.wixstatic.com/media/1e04d5_d64b6442d483498f9da713cd9a98ab22~mv2.jpg/v1/fill/w_550,h_355,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_d64b6442d483498f9da713cd9a98ab22~mv2.jpg)
