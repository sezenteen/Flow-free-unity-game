# Flow Free clone (Dot connection puzzle)

Unity дээр хийсэн Flow Free тоглоомын clone хувилбар. Зааврын дагуу 5 level-тэй, тоглогч ижил өнгийн хоёр цэгийг хооронд нь зураасаар холбох puzzle төрлийн тоглоом.

## Тоглоомын зорилго

Талбар дээр байгаа ижил өнгийн хоёр цэгийг хооронд нь холбоно. Нэг өнгийн зам нөгөө өнгийн замтай давхцахгүй байх ёстой. Бүх өнгийн хос цэгүүдийг зөв холбовол level дуусна.

## Хэрхэн ажиллуулах вэ

1. Unity Hub нээнэ.
2. `D:\FlowFree` folder-ийг Unity project гэж нээнэ.
3. `Assets/Project/Scenes/MainMenu.unity` scene-ийг нээнэ.
4. Unity Editor дээр `Play` товч дарна.
5. Main menu дээрээс `LEVEL 1`-ээс `LEVEL 5` хүртэл сонгож тоглоно.

## Хэрхэн тоглох вэ

1. Level сонгоно.
2. Ижил өнгийн нэг цэг дээр mouse дарж эхэлнэ.
3. Mouse-аа чирж нөгөө ижил өнгийн цэг хүртэл холбоно.
4. Буруу зурсан бол `RESTART` дарж дахин эхэлж болно.
5. Level дууссаны дараа `NEXT` дарж дараагийн level рүү орно.
6. `MENU` дарвал level сонгох дэлгэц рүү буцна.

## Төслийн бүтэц

- `Assets/Project/Scenes/MainMenu.unity` - тоглоом эхлэх scene.
- `Assets/Project/Scenes/Gameplay.unity` - тоглоомын scene.
- `Assets/Project/Scripts/SimpleFlowGame.cs` - menu, level, board, mouse input, win condition бүгдийг удирддаг үндсэн script.
- `Assets/Resources/Sprites/` - board, cell, circle зэрэг sprite зургууд.
- `.gitignore` - Unity-ийн автоматаар үүсдэг том cache болон build файлуудыг GitHub руу оруулахгүй байхаар тохируулсан файл.

## Тайлбар

Миний хувьд маш сонирхолтой туршлага боллоо гэж бодож байна. Тоглоом хөгжүүлэлтийн талаар багахан мэдлэгтэй байсан ч 7 хоногийн хугацаанд их зүйлийг судалж, сурч бас мэдлээ. Энэ тоглоомыг хийхэд YouTube: https://www.youtube.com/watch?v=Id23yLF2zag&list=PLsSHiYJvCZFfHN3opoV_ePrW7OHPs8u9M&index=2 хаягаас үзэж, эхний 3 хоног дуйрааж хийх гэж оролдсон гэхдээ бүрэн ажиллуулж чадаагүй тул Codex AI ашигласан болно. Мөн Game asset-ыг https://github.com/zerefgd/Connect хаягаас авсан болно.
