#define _CRT_SECURE_NO_WARNINGS
#define _SCL_SECURE_NO_WARNINGS

#include <fstream>
#include <iostream>
#include <string>
#include <vector>

#define STB_IMAGE_IMPLEMENTATION
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "stb_image.h"
#include "stb_image_write.h"

typedef unsigned short codepoint;

struct Char
{
	codepoint codePoint;
	unsigned x, y, width, height;
};

int main(int argc, char* argv[])
{
	if (argc < 4)
	{
		std::cout << argv[0] << " <metadata file> <image file> <output file>\n";
		return 1;
	}

	std::ifstream metaFin (argv[1], std::ios::binary);
	if (!metaFin.is_open())
		return 2;

	int width, height, cmp;
	auto srcPixels = stbi_load(argv[2], &width, &height, &cmp, 0);
	if (srcPixels == nullptr)
		return 3;

	std::vector<Char> chars;
	std::string line;
	Char current;
	while (std::getline(metaFin, line))
	{
		current.codePoint = line[0];
		//todo: handle utf-16 (codePoint > 0x7f)

		char* next = &line[2];
		current.x = std::strtoul(next, &next, 10);
		current.y = std::strtoul(next, &next, 10);
		current.width = std::strtoul(next, &next, 10);
		current.height = std::strtoul(next, &next, 10);
		chars.push_back(current);
	}
	unsigned nChars = chars.size();

	unsigned cmp1 = cmp - 1;

	//calculate the number of pixel rows of the image will be reserved for metadata
	unsigned rows = 4;
	rows += ((nChars * sizeof(codepoint)) + cmp1) & ~cmp1;
	rows += nChars * (sizeof(unsigned) * 4);
	rows = 1 + ((rows - 1) / (cmp * width)); //round up

	//create resized image with metadata rows at the top
	unsigned char* dstPixels = new unsigned char[width * (height + rows) * cmp];
	std::fill(dstPixels, dstPixels + (width * rows * cmp), 0);
	std::copy(srcPixels, srcPixels + width * height * cmp, dstPixels + (width * rows * cmp));
	stbi_image_free(srcPixels);

	((unsigned*)dstPixels)[0] = nChars;

	unsigned cpStart = sizeof(unsigned);
	unsigned rgnStart = cpStart + (sizeof(codepoint) * nChars);

	for (unsigned i = 0; i < nChars; i++)
	{
		((codepoint*)(dstPixels + cpStart))[i] = chars[i].codePoint;

		unsigned* start = (unsigned*)(dstPixels + rgnStart) + (i * 4);
		*start = chars[i].x;
		*(start + 1) = chars[i].y + rows;
		*(start + 2) = chars[i].width;
		*(start + 3) = chars[i].height;
	}

	stbi_write_png(argv[3], width, height + rows, cmp, dstPixels, width * cmp);
	delete[] dstPixels;

	//verification test
	/*std::vector<Char> chars2;
	int width2, height2, cmp2;
	auto srcPixels2 = stbi_load(argv[3], &width2, &height2, &cmp2, 0);

	unsigned nChars2 = ((unsigned*)srcPixels2)[0];
	unsigned cpStart2 = sizeof(unsigned);
	unsigned rgnStart2 = cpStart + (sizeof(codepoint) * nChars2);
	std::ofstream fout(std::string(argv[3]) + ".txt");
	for (auto i = 0; i < nChars2; i++)
	{
		current.codePoint = ((codepoint*)(srcPixels2 + cpStart2))[i];

		unsigned* start = (unsigned*)(srcPixels2 + rgnStart) + (i * 4);
		current.x = *start;
		current.y = *(start + 1);
		current.width = *(start + 2);
		current.height = *(start + 3);

		fout << (char)current.codePoint << " " << current.x << " " << current.y << " " << current.width << " " << current.height << "\n";
	}
	fout.close();
	stbi_image_free(srcPixels2);*/

	return 0;
}
