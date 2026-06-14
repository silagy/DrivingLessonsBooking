import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

const skyGradient = 'linear-gradient(135deg, #0057FF 0%, #00BBC7 100%)';
const skyGradientDark = 'linear-gradient(135deg, #003293 0%, #007A82 100%)';

export const AppPreset = definePreset(Aura, {
  primitive: {
    sky: {
      50: '#EAF1FF',
      100: '#D7E3FF',
      200: '#B3CCFF',
      300: '#7BAEFF',
      400: '#3D82FF',
      500: '#0057FF',
      600: '#004AD6',
      700: '#003293',
      800: '#00276F',
      900: '#001F5C',
      950: '#001233',
    },
    ocean: {
      50: '#E5FBFC',
      100: '#C5F6F9',
      200: '#7BF4FB',
      300: '#3DE3EC',
      400: '#12CDD8',
      500: '#00BBC7',
      600: '#00A0AB',
      700: '#007A82',
      800: '#006066',
      900: '#004D52',
      950: '#00282B',
    },
    steel: {
      100: '#DBE3F3',
      200: '#C3CFE5',
      300: '#98A5BD',
    },
    whale: {
      500: '#005389',
    },
  },
  semantic: {
    primary: {
      50: '{sky.50}',
      100: '{sky.100}',
      200: '{sky.200}',
      300: '{sky.300}',
      400: '{sky.400}',
      500: '{sky.500}',
      600: '{sky.600}',
      700: '{sky.700}',
      800: '{sky.800}',
      900: '{sky.900}',
      950: '{sky.950}',
    },
    colorScheme: {
      light: {
        primary: {
          color: '{sky.500}',
          contrastColor: '#ffffff',
          hoverColor: '{sky.700}',
          activeColor: '{sky.800}',
        },
        surface: {
          0: '#ffffff',
          50: '#FBFBFF',
          100: '#F5F6FF',
          200: '#E6E8F6',
          300: '#C3CFE5',
          400: '#9FA9BB',
          500: '#52555C',
          600: '#43464D',
          700: '#2C2F37',
          800: '#1B1D22',
          900: '#121418',
          950: '#0B0C0E',
        },
      },
    },
  },
  components: {
    button: {
      root: {
        borderRadius: '999px',
        paddingX: '1.625rem',
        paddingY: '0.7rem',
        gap: '0.55rem',
        label: { fontWeight: '600' },
      },
      colorScheme: {
        light: {
          root: {
            primary: {
              background: skyGradient,
              hoverBackground: skyGradientDark,
              activeBackground: skyGradientDark,
              borderColor: 'transparent',
              hoverBorderColor: 'transparent',
              activeBorderColor: 'transparent',
              color: '#ffffff',
              hoverColor: '#ffffff',
              activeColor: '#ffffff',
            },
          },
        },
      },
    },
  },
});
